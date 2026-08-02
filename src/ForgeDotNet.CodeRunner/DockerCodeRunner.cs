using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForgeDotNet.Application.CodeRunner;

namespace ForgeDotNet.CodeRunner;

public sealed partial class DockerCodeRunner : ICodeRunner, IDisposable
{
    private const int MaximumImageBytes = 900 * 1024 * 1024;
    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, byte> _activeContainers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _activeWorkspaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly DockerCodeRunnerOptions _options;
    private readonly IDockerRunSpecificationSource _specificationSource;
    private readonly TimeProvider _timeProvider;
    private readonly DockerCli _docker;
    private readonly SemaphoreSlim _concurrency;
    private readonly SemaphoreSlim _maintenance = new(1, 1);
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _disposed;

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{2,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SuiteIdPattern();

    public DockerCodeRunner(
        DockerCodeRunnerOptions options,
        IDockerRunSpecificationSource specificationSource,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(specificationSource);
        ArgumentNullException.ThrowIfNull(timeProvider);
        options.Validate();
        _options = options;
        _specificationSource = specificationSource;
        _timeProvider = timeProvider;
        _docker = new DockerCli(options);
        _concurrency = new SemaphoreSlim(options.MaximumConcurrency, options.MaximumConcurrency);
    }

    public ValueTask<CodeRunResult> RunAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CodeRunContract.ValidateRequest(request);
        string fingerprint = ComputeFingerprint(request);
        var candidate = new CacheEntry(
            fingerprint,
            new Lazy<Task<CodeRunResult>>(
                () => ExecuteAsync(request, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));
        CacheEntry entry = _cache.GetOrAdd(request.RequestId, candidate);
        if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Un identifiant de requête runner ne peut pas être réutilisé avec un autre contenu.");
        }

        return new ValueTask<CodeRunResult>(entry.Execution.Value);
    }

    private async Task<CodeRunResult> ExecuteAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = _timeProvider.GetUtcNow();
        DockerRunSpecification? specification = await _specificationSource.GetAsync(request, cancellationToken);
        if (specification is null)
        {
            return Unavailable(
                request,
                startedAtUtc,
                "Aucune suite de tests approuvée n’est disponible pour cet exercice ; utilisez l’export manuel.");
        }

        if (!SuiteIdPattern().IsMatch(specification.SuiteId ?? string.Empty))
        {
            throw new InvalidDataException("La source de tests approuvée a fourni un identifiant de suite invalide.");
        }

        try
        {
            await _concurrency.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled(request, startedAtUtc, compilationSucceeded: false);
        }

        var resources = new AttemptResources();
        CodeRunResult? executionResult = null;
        Exception? executionFailure = null;
        try
        {
            executionResult = await RunIsolatedAttemptAsync(
                request,
                specification,
                startedAtUtc,
                resources,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            executionResult = Cancelled(request, startedAtUtc, compilationSucceeded: false);
        }
        catch (DockerUnavailableException exception)
        {
            executionResult = Unavailable(request, startedAtUtc, exception.Message);
        }
        catch (Exception exception)
        {
            executionFailure = exception;
        }

        List<Exception> cleanupFailures;
        try
        {
            cleanupFailures = await CleanupAttemptAsync(resources);
        }
        finally
        {
            _concurrency.Release();
        }

        if (cleanupFailures.Count > 0)
        {
            if (executionFailure is not null)
            {
                cleanupFailures.Insert(0, executionFailure);
            }

            throw new AggregateException(
                "L’exécution et tous ses nettoyages n’ont pas pu être prouvés.",
                cleanupFailures);
        }

        if (executionFailure is not null)
        {
            ExceptionDispatchInfo.Capture(executionFailure).Throw();
        }

        return executionResult
            ?? throw new InvalidOperationException("Le runner n’a produit aucun résultat interne.");
    }

    private async Task<CodeRunResult> RunIsolatedAttemptAsync(
        CodeRunRequest request,
        DockerRunSpecification specification,
        DateTimeOffset startedAtUtc,
        AttemptResources resources,
        CancellationToken cancellationToken)
    {
        await RunMaintenanceAsync(cancellationToken);
        await EnsureImagePolicyAsync(cancellationToken);

        WorkspaceCreation workspace = await CreateWorkspaceAsync(request, specification, cancellationToken);
        resources.WorkspacePath = workspace.Path;
        resources.StandardInput = workspace.StandardInput;
        resources.ContainerName = $"forge-dotnet-runner-{request.RequestId:N}";
        _activeContainers.TryAdd(resources.ContainerName, 0);

        IReadOnlyList<string> createArguments = BuildCreateArguments(
            request,
            resources.WorkspacePath,
            resources.ContainerName,
            resources.StandardInput is not null);
        resources.ContainerMayExist = true;
        DockerCommandResult create = await _docker.RunAsync(
            createArguments,
            _options.DockerControlTimeout,
            cancellationToken);
        if (create.Cancelled)
        {
            return Cancelled(request, startedAtUtc, compilationSucceeded: false);
        }

        if (!create.Succeeded)
        {
            return Unavailable(
                request,
                startedAtUtc,
                "Docker a refusé la politique d’isolation ; l’exécution est fermée par sécurité.");
        }

        TimeSpan executionTimeout = _options.CompilationTimeout
            + _options.TestTimeout
            + TimeSpan.FromSeconds(5);
        IReadOnlyList<string> startArguments = resources.StandardInput is null
            ? ["start", "--attach", resources.ContainerName]
            : ["start", "--attach", "--interactive", resources.ContainerName];
        DockerCommandResult execution = resources.StandardInput is null
            ? await _docker.RunAsync(startArguments, executionTimeout, cancellationToken)
            : await _docker.RunAsync(startArguments, executionTimeout, resources.StandardInput, cancellationToken);
        resources.StandardInput = null;
        RunnerEnvelope? compilation = ParseMessages(execution.StandardOutput)
            .LastOrDefault(message => string.Equals(message.Kind, "compilation", StringComparison.Ordinal));
        bool compilationSucceeded = compilation is not null
            && string.Equals(compilation.Status, "succeeded", StringComparison.Ordinal);
        if (execution.Cancelled)
        {
            return Cancelled(request, startedAtUtc, compilationSucceeded);
        }

        if (execution.TimedOut)
        {
            return TimedOut(request, startedAtUtc, compilationSucceeded);
        }

        ContainerState state = await InspectContainerStateAsync(resources.ContainerName, CancellationToken.None);
        if (state.OomKilled)
        {
            return TimedOut(request, startedAtUtc, compilationSucceeded);
        }

        RunnerEnvelope? result = ParseMessages(execution.StandardOutput)
            .LastOrDefault(message => string.Equals(message.Kind, "result", StringComparison.Ordinal));
        if (result is null)
        {
            return Unavailable(
                request,
                startedAtUtc,
                $"Le conteneur n’a fourni aucun résultat structuré vérifiable (code interne {state.ExitCode}).");
        }

        return MapResult(request, startedAtUtc, compilation, result);
    }

    private async Task<List<Exception>> CleanupAttemptAsync(AttemptResources resources)
    {
        var failures = new List<Exception>(2);
        if (resources.ContainerMayExist && resources.ContainerName is not null)
        {
            try
            {
                await RemoveContainerVerifiedAsync(resources.ContainerName);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        if (resources.ContainerName is not null)
        {
            _activeContainers.TryRemove(resources.ContainerName, out _);
        }

        if (resources.WorkspacePath is not null)
        {
            try
            {
                DeleteWorkspaceVerified(resources.WorkspacePath);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            finally
            {
                _activeWorkspaces.TryRemove(resources.WorkspacePath, out _);
            }
        }

        return failures;
    }

    private async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        await _maintenance.WaitAsync(cancellationToken);
        try
        {
            await CleanupOrphanedContainersAsync(cancellationToken);
            CleanupOrphanedWorkspaces();
        }
        finally
        {
            _maintenance.Release();
        }
    }

    private async Task CleanupOrphanedContainersAsync(CancellationToken cancellationToken)
    {
        DockerCommandResult list = await _docker.RunAsync(
            [
                "ps",
                "--all",
                "--format",
                "{{.Names}}",
                "--filter",
                $"label={DockerCodeRunnerOptions.RunnerLabel}={DockerCodeRunnerOptions.RunnerLabelValue}",
            ],
            _options.DockerControlTimeout,
            cancellationToken);
        if (!list.Succeeded)
        {
            throw new DockerUnavailableException("Docker est indisponible ; aucun conteneur n’a été créé.");
        }

        string[] names = list.StandardOutput.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string name in names)
        {
            if (!name.StartsWith("forge-dotnet-runner-", StringComparison.Ordinal)
                || name.Length != "forge-dotnet-runner-".Length + 32)
            {
                throw new InvalidDataException("Un conteneur portant le label Forge.NET a un nom inattendu.");
            }

            if (!_activeContainers.ContainsKey(name))
            {
                await RemoveContainerVerifiedAsync(name);
            }
        }
    }

    private void CleanupOrphanedWorkspaces()
    {
        string root = EnsureWorkspaceRoot();
        foreach (string directory in Directory.EnumerateDirectories(root, "run-*", SearchOption.TopDirectoryOnly))
        {
            string fullPath = Path.GetFullPath(directory);
            EnsureDirectChild(root, fullPath);
            if (!_activeWorkspaces.ContainsKey(fullPath))
            {
                DeleteWorkspaceVerified(fullPath);
            }
        }
    }

    private async Task EnsureImagePolicyAsync(CancellationToken cancellationToken)
    {
        DockerCommandResult inspect = await _docker.RunAsync(
            ["image", "inspect", _options.ImageReference],
            _options.DockerControlTimeout,
            cancellationToken);
        if (!inspect.Succeeded)
        {
            throw new DockerUnavailableException("L’image runner immuable n’est pas disponible localement.");
        }

        using JsonDocument document = JsonDocument.Parse(inspect.StandardOutput);
        JsonElement image = document.RootElement.ValueKind == JsonValueKind.Array
            && document.RootElement.GetArrayLength() == 1
            ? document.RootElement[0]
            : throw new InvalidDataException("La réponse d’inspection de l’image Docker est invalide.");
        string? imageId = image.GetProperty("Id").GetString();
        string? operatingSystem = image.GetProperty("Os").GetString();
        long size = image.GetProperty("Size").GetInt64();
        JsonElement configuration = image.GetProperty("Config");
        string? user = configuration.GetProperty("User").GetString();
        string? workingDirectory = configuration.GetProperty("WorkingDir").GetString();
        string[] entrypoint = configuration.GetProperty("Entrypoint")
            .EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
        bool versionLabelValid = configuration.TryGetProperty("Labels", out JsonElement labels)
            && labels.ValueKind == JsonValueKind.Object
            && labels.TryGetProperty("org.opencontainers.image.version", out JsonElement version)
            && string.Equals(version.GetString(), "04C", StringComparison.Ordinal);
        bool exposesPorts = configuration.TryGetProperty("ExposedPorts", out JsonElement ports)
            && ports.ValueKind == JsonValueKind.Object
            && ports.EnumerateObject().Any();
        bool declaresVolumes = configuration.TryGetProperty("Volumes", out JsonElement volumes)
            && volumes.ValueKind == JsonValueKind.Object
            && volumes.EnumerateObject().Any();

        if (!string.Equals(imageId, _options.ImageReference, StringComparison.Ordinal)
            || !string.Equals(operatingSystem, "linux", StringComparison.Ordinal)
            || !string.Equals(user, DockerCodeRunnerOptions.RequiredContainerUser, StringComparison.Ordinal)
            || !string.Equals(workingDirectory, "/workspace", StringComparison.Ordinal)
            || entrypoint.Length != 2
            || !string.Equals(entrypoint[0], "dotnet", StringComparison.Ordinal)
            || !string.Equals(entrypoint[1], "/opt/forge-runner/ForgeDotNet.RunnerHost.dll", StringComparison.Ordinal)
            || size is <= 0 or > MaximumImageBytes
            || !versionLabelValid
            || exposesPorts
            || declaresVolumes)
        {
            throw new InvalidDataException("L’image runner ne satisfait pas la politique 04C.");
        }
    }

    private async Task<WorkspaceCreation> CreateWorkspaceAsync(
        CodeRunRequest request,
        DockerRunSpecification specification,
        CancellationToken cancellationToken)
    {
        string root = EnsureWorkspaceRoot();
        string workspace = Path.Combine(
            root,
            $"run-{request.RequestId:N}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant()}");
        EnsureDirectChild(root, workspace);
        _activeWorkspaces.TryAdd(workspace, 0);
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(Path.Combine(workspace, "sources"));
        string? standardInput = null;
        try
        {
            foreach (CodeRunSourceFile source in request.SourceFiles)
            {
                string destination = Path.Combine(workspace, "sources", source.FileName);
                await File.WriteAllTextAsync(destination, source.Content, new UTF8Encoding(false, true), cancellationToken);
            }

            string manifest = JsonSerializer.Serialize(new ContainerRequest(
                request.RequestId,
                specification.SuiteId,
                request.SourceFiles.Select(source => source.FileName).ToArray(),
                specification.SuiteDefinition is not null));
            await File.WriteAllTextAsync(
                Path.Combine(workspace, "request.json"),
                manifest,
                new UTF8Encoding(false, true),
                cancellationToken);
            if (specification.SuiteDefinition is not null)
            {
                if (Encoding.UTF8.GetByteCount(specification.SuiteDefinition) > 256 * 1024)
                {
                    throw new InvalidDataException("La suite runner approuvée est trop volumineuse.");
                }

                byte[] key = RandomNumberGenerator.GetBytes(32);
                byte[] nonce = RandomNumberGenerator.GetBytes(12);
                byte[] plaintext = Encoding.UTF8.GetBytes(specification.SuiteDefinition);
                byte[] ciphertext = new byte[plaintext.Length];
                byte[] tag = new byte[16];
                using (var aes = new AesGcm(key, tag.Length))
                {
                    aes.Encrypt(nonce, plaintext, ciphertext, tag);
                }

                byte[] envelope = new byte[nonce.Length + tag.Length + ciphertext.Length];
                nonce.CopyTo(envelope, 0);
                tag.CopyTo(envelope, nonce.Length);
                ciphertext.CopyTo(envelope, nonce.Length + tag.Length);
                await File.WriteAllBytesAsync(Path.Combine(workspace, "suite.bin"), envelope, cancellationToken);
                standardInput = Convert.ToBase64String(key);
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plaintext);
            }
            return new WorkspaceCreation(workspace, standardInput);
        }
        catch
        {
            DeleteWorkspaceVerified(workspace);
            _activeWorkspaces.TryRemove(workspace, out _);
            throw;
        }
    }

    private ReadOnlyCollection<string> BuildCreateArguments(
        CodeRunRequest request,
        string workspacePath,
        string containerName,
        bool interactive)
    {
        string cpu = _options.CpuCount.ToString("0.###", CultureInfo.InvariantCulture);
        string memory = _options.MemoryBytes.ToString(CultureInfo.InvariantCulture);
        string workspaceBytes = _options.WorkspaceBytes.ToString(CultureInfo.InvariantCulture);
        string mount = $"type=bind,src={workspacePath},dst=/input,readonly,bind-nonrecursive";
        string tmpfs = $"rw,nosuid,nodev,noexec,size={workspaceBytes},uid=1654,gid=1654,mode=0700";
        var arguments = new List<string>
        {
            "create",
            "--name", containerName,
            "--hostname", "forge-runner",
            "--label", $"{DockerCodeRunnerOptions.RunnerLabel}={DockerCodeRunnerOptions.RunnerLabelValue}",
            "--label", $"forge-dotnet.runner.owner={_ownerId}",
            "--label", $"forge-dotnet.runner.request={request.RequestId:N}",
            "--pull", "never",
            "--network", "none",
            "--read-only",
            "--user", DockerCodeRunnerOptions.RequiredContainerUser,
            "--cap-drop", "ALL",
            "--security-opt", "no-new-privileges=true",
            "--security-opt", "seccomp=builtin",
            "--memory", memory,
            "--memory-swap", memory,
            "--cpus", cpu,
            "--pids-limit", _options.PidsLimit.ToString(CultureInfo.InvariantCulture),
            "--ulimit", "nofile=256:256",
            "--ipc", "none",
            "--init",
            "--stop-timeout", "1",
            "--log-driver", "none",
            "--mount", mount,
            "--tmpfs", $"/workspace:{tmpfs}",
            "--tmpfs", "/tmp:rw,nosuid,nodev,noexec,size=16777216,uid=1654,gid=1654,mode=0700",
            "--env", "HOME=/workspace/home",
            "--env", "DOTNET_CLI_HOME=/workspace/.dotnet",
            "--env", "NUGET_PACKAGES=/workspace/.nuget/packages",
            "--env", "DOTNET_EnableDiagnostics=0",
            "--env", "DOTNET_NOLOGO=1",
            "--env", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1",
            "--env", "DOTNET_CLI_TELEMETRY_OPTOUT=1",
            "--env", $"FORGE_RUNNER_COMPILATION_TIMEOUT_MS={(long)_options.CompilationTimeout.TotalMilliseconds}",
            "--env", $"FORGE_RUNNER_TEST_TIMEOUT_MS={(long)_options.TestTimeout.TotalMilliseconds}",
            "--workdir", "/workspace",
        };
        if (interactive)
        {
            arguments.Add("--interactive");
        }

        arguments.Add(_options.ImageReference);
        return Array.AsReadOnly(arguments.ToArray());
    }

    private async Task<ContainerState> InspectContainerStateAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        DockerCommandResult inspect = await _docker.RunAsync(
            ["inspect", "--format", "{{json .State}}", containerName],
            _options.DockerControlTimeout,
            cancellationToken);
        if (!inspect.Succeeded)
        {
            throw new DockerUnavailableException("L’état final du conteneur n’a pas pu être inspecté.");
        }

        ContainerState? state = JsonSerializer.Deserialize<ContainerState>(inspect.StandardOutput.Trim(), JsonOptions);
        return state ?? throw new InvalidDataException("L’état final Docker est invalide.");
    }

    private async Task RemoveContainerVerifiedAsync(string containerName)
    {
        DockerCommandResult remove = await _docker.RunAsync(
            ["rm", "--force", containerName],
            _options.DockerControlTimeout,
            CancellationToken.None);
        bool absentBeforeRemoval = remove.StandardError.Contains("No such container", StringComparison.OrdinalIgnoreCase);
        DockerCommandResult verify = await _docker.RunAsync(
            ["inspect", containerName],
            _options.DockerControlTimeout,
            CancellationToken.None);
        if (!verify.Succeeded
            && verify.StandardError.Contains("No such", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        DockerCommandResult list = await _docker.RunAsync(
            [
                "ps",
                "--all",
                "--format", "{{.Names}}",
                "--filter", $"name=^{containerName}$",
            ],
            _options.DockerControlTimeout,
            CancellationToken.None);
        bool absentFromList = list.Succeeded
            && !list.StandardOutput.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(containerName, StringComparer.Ordinal);
        if (absentFromList)
        {
            return;
        }

        if (!remove.Succeeded && !absentBeforeRemoval)
        {
            throw new InvalidOperationException("Le conteneur runner n’a pas pu être supprimé ni prouvé absent.");
        }

        throw new InvalidOperationException("La suppression du conteneur runner n’a pas pu être prouvée.");
    }

    private string EnsureWorkspaceRoot()
    {
        string root = Path.GetFullPath(_options.WorkspaceRootPath);
        Directory.CreateDirectory(root);
        FileAttributes attributes = File.GetAttributes(root);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("La racine de workspace runner ne peut pas être un point de réanalyse.");
        }

        return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void EnsureDirectChild(string root, string candidate)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullCandidate = Path.GetFullPath(candidate);
        if (!string.Equals(Path.GetDirectoryName(fullCandidate), fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Le workspace runner sort de sa racine dédiée.");
        }
    }

    private void DeleteWorkspaceVerified(string workspacePath)
    {
        string root = Path.GetFullPath(_options.WorkspaceRootPath);
        string fullPath = Path.GetFullPath(workspacePath);
        EnsureDirectChild(root, fullPath);
        if (Directory.Exists(fullPath))
        {
            FileAttributes attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Un workspace runner est devenu un point de réanalyse.");
            }

            Directory.Delete(fullPath, recursive: true);
        }

        if (Directory.Exists(fullPath))
        {
            throw new InvalidOperationException("Le nettoyage du workspace runner n’a pas pu être prouvé.");
        }
    }

    private CodeRunResult MapResult(
        CodeRunRequest request,
        DateTimeOffset startedAtUtc,
        RunnerEnvelope? compilation,
        RunnerEnvelope result)
    {
        CodeRunResult mapped = result.Status switch
        {
            "succeeded" => new CodeRunResult(
                request.RequestId,
                CodeRunStatus.Succeeded,
                SuccessfulCompilation(compilation),
                MapTests(result, CodeRunStageStatus.Succeeded),
                "Compilation et tests terminés dans un conteneur isolé ; ce résultat ne constitue pas une preuve de maîtrise.",
                Guid.NewGuid(),
                startedAtUtc,
                _timeProvider.GetUtcNow()),
            "compilation-failed" => new CodeRunResult(
                request.RequestId,
                CodeRunStatus.CompilationFailed,
                new CodeCompilationResult(
                    CodeRunStageStatus.Failed,
                    MapDiagnostics(result.Diagnostics),
                    new CodeRunTextOutput(result.CompilationOutput ?? string.Empty, result.CompilationOutputTruncated)),
                NotRunTests(),
                "La compilation isolée a échoué ; les tests n’ont pas été lancés.",
                Guid.NewGuid(),
                startedAtUtc,
                _timeProvider.GetUtcNow()),
            "tests-failed" => new CodeRunResult(
                request.RequestId,
                CodeRunStatus.TestsFailed,
                SuccessfulCompilation(compilation),
                MapTests(result, CodeRunStageStatus.Failed),
                "La compilation isolée a réussi, puis un ou plusieurs tests ont échoué.",
                Guid.NewGuid(),
                startedAtUtc,
                _timeProvider.GetUtcNow()),
            "timed-out" when string.Equals(result.Stage, "tests", StringComparison.Ordinal) =>
                TimedOut(request, startedAtUtc, compilationSucceeded: true, result.TestOutput, result.TestOutputTruncated),
            "timed-out" => TimedOut(
                request,
                startedAtUtc,
                compilationSucceeded: false,
                result.CompilationOutput,
                result.CompilationOutputTruncated),
            "unavailable" => Unavailable(
                request,
                startedAtUtc,
                string.IsNullOrWhiteSpace(result.CompilationOutput)
                    ? "Le runner isolé a refusé la demande ; aucune validation automatique n’a eu lieu."
                    : result.CompilationOutput),
            _ => throw new InvalidDataException("Le conteneur a renvoyé un statut runner inconnu."),
        };
        return CodeRunContract.NormalizeResult(request, mapped);
    }

    private static CodeCompilationResult SuccessfulCompilation(RunnerEnvelope? compilation) => new(
        CodeRunStageStatus.Succeeded,
        Array.Empty<CodeRunnerDiagnostic>(),
        new CodeRunTextOutput(
            compilation?.CompilationOutput ?? "Compilation isolée réussie.",
            compilation?.CompilationOutputTruncated ?? false));

    private static CodeTestResult MapTests(RunnerEnvelope result, CodeRunStageStatus status) => new(
        status,
        result.TotalCount,
        result.PassedCount,
        result.FailedCount,
        result.HiddenFailureCount,
        result.HiddenFailureCount > 0,
        Array.AsReadOnly((result.VisibleFailures ?? [])
            .Take(CodeRunContract.MaximumVisibleFailureCount)
            .Select(failure => new VisibleTestFailure(failure.Name, failure.Message))
            .ToArray()),
        new CodeRunTextOutput(result.TestOutput ?? string.Empty, result.TestOutputTruncated));

    private static ReadOnlyCollection<CodeRunnerDiagnostic> MapDiagnostics(
        IReadOnlyList<RunnerDiagnosticEnvelope>? diagnostics) => Array.AsReadOnly((diagnostics ?? [])
        .Take(CodeRunContract.MaximumDiagnosticCount)
        .Select(diagnostic => new CodeRunnerDiagnostic(
            diagnostic.Code,
            string.Equals(diagnostic.Severity, "warning", StringComparison.OrdinalIgnoreCase)
                ? CodeRunnerDiagnosticSeverity.Warning
                : string.Equals(diagnostic.Severity, "info", StringComparison.OrdinalIgnoreCase)
                    ? CodeRunnerDiagnosticSeverity.Info
                    : CodeRunnerDiagnosticSeverity.Error,
            diagnostic.Message,
            diagnostic.FileName,
            diagnostic.Line,
            diagnostic.Column))
        .ToArray());

    private static List<RunnerEnvelope> ParseMessages(string standardOutput)
    {
        var messages = new List<RunnerEnvelope>();
        foreach (string line in standardOutput.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith('{'))
            {
                continue;
            }

            try
            {
                RunnerEnvelope? message = JsonSerializer.Deserialize<RunnerEnvelope>(line, JsonOptions);
                if (message is not null)
                {
                    messages.Add(message);
                }
            }
            catch (JsonException)
            {
            }
        }

        return messages;
    }

    private CodeRunResult Cancelled(
        CodeRunRequest request,
        DateTimeOffset startedAtUtc,
        bool compilationSucceeded) => Terminal(
            request,
            CodeRunStatus.Cancelled,
            compilationSucceeded ? CodeRunStageStatus.Succeeded : CodeRunStageStatus.Cancelled,
            compilationSucceeded ? CodeRunStageStatus.Cancelled : CodeRunStageStatus.NotRun,
            "Exécution Docker annulée ; le conteneur a été supprimé.",
            startedAtUtc);

    private CodeRunResult TimedOut(
        CodeRunRequest request,
        DateTimeOffset startedAtUtc,
        bool compilationSucceeded,
        string? output = null,
        bool outputTruncated = false)
    {
        CodeRunResult result = compilationSucceeded
            ? new CodeRunResult(
                request.RequestId,
                CodeRunStatus.TimedOut,
                new CodeCompilationResult(
                    CodeRunStageStatus.Succeeded,
                    Array.Empty<CodeRunnerDiagnostic>(),
                    new CodeRunTextOutput("Compilation isolée réussie.", false)),
                new CodeTestResult(
                    CodeRunStageStatus.TimedOut,
                    0,
                    0,
                    0,
                    0,
                    false,
                    Array.Empty<VisibleTestFailure>(),
                    new CodeRunTextOutput(output ?? "Délai des tests atteint.", outputTruncated)),
                "Le délai des tests isolés a été atteint ; le conteneur a été supprimé.",
                Guid.NewGuid(),
                startedAtUtc,
                _timeProvider.GetUtcNow())
            : Terminal(
                request,
                CodeRunStatus.TimedOut,
                CodeRunStageStatus.TimedOut,
                CodeRunStageStatus.NotRun,
                output ?? "Délai de compilation atteint ; le conteneur a été supprimé.",
                startedAtUtc,
                outputTruncated);
        return CodeRunContract.NormalizeResult(request, result);
    }

    private CodeRunResult Unavailable(
        CodeRunRequest request,
        DateTimeOffset startedAtUtc,
        string message) => Terminal(
            request,
            CodeRunStatus.Unavailable,
            CodeRunStageStatus.Unavailable,
            CodeRunStageStatus.NotRun,
            message,
            startedAtUtc);

    private CodeRunResult Terminal(
        CodeRunRequest request,
        CodeRunStatus status,
        CodeRunStageStatus compilationStatus,
        CodeRunStageStatus testStatus,
        string message,
        DateTimeOffset startedAtUtc,
        bool outputTruncated = false)
    {
        var result = new CodeRunResult(
            request.RequestId,
            status,
            new CodeCompilationResult(
                compilationStatus,
                Array.Empty<CodeRunnerDiagnostic>(),
                new CodeRunTextOutput(message, outputTruncated)),
            new CodeTestResult(
                testStatus,
                0,
                0,
                0,
                0,
                false,
                Array.Empty<VisibleTestFailure>(),
                new CodeRunTextOutput(string.Empty, false)),
            message,
            Guid.NewGuid(),
            startedAtUtc,
            _timeProvider.GetUtcNow());
        return CodeRunContract.NormalizeResult(request, result);
    }

    private static CodeTestResult NotRunTests() => new(
        CodeRunStageStatus.NotRun,
        0,
        0,
        0,
        0,
        false,
        Array.Empty<VisibleTestFailure>(),
        new CodeRunTextOutput(string.Empty, false));

    private static string ComputeFingerprint(CodeRunRequest request)
    {
        var builder = new StringBuilder();
        Append(builder, request.ExerciseId);
        Append(builder, request.ExerciseVersion.ToString(CultureInfo.InvariantCulture));
        Append(builder, request.ContentRevision);
        foreach (CodeRunSourceFile sourceFile in request.SourceFiles)
        {
            Append(builder, sourceFile.FileName);
            Append(builder, sourceFile.Content);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void Append(StringBuilder builder, string value) => builder
        .Append(value.Length)
        .Append(':')
        .Append(value)
        .Append('|');

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _concurrency.Dispose();
        _maintenance.Dispose();
    }

    private sealed record CacheEntry(string Fingerprint, Lazy<Task<CodeRunResult>> Execution);

    private sealed record WorkspaceCreation(string Path, string? StandardInput);

    private sealed class AttemptResources
    {
        public string? WorkspacePath { get; set; }

        public string? ContainerName { get; set; }

        public bool ContainerMayExist { get; set; }

        public string? StandardInput { get; set; }
    }

    private sealed record ContainerRequest(
        Guid RequestId,
        string SuiteId,
        IReadOnlyList<string> SourceFiles,
        bool HasEncryptedSuite);

    private sealed record ContainerState(bool OomKilled, int ExitCode, string? Error);

    private sealed record RunnerEnvelope(
        string Kind,
        string Status,
        string Stage,
        IReadOnlyList<RunnerDiagnosticEnvelope>? Diagnostics,
        string? CompilationOutput,
        bool CompilationOutputTruncated,
        int TotalCount,
        int PassedCount,
        int FailedCount,
        int HiddenFailureCount,
        IReadOnlyList<PublicFailureEnvelope>? VisibleFailures,
        string? TestOutput,
        bool TestOutputTruncated);

    private sealed record RunnerDiagnosticEnvelope(
        string Code,
        string Severity,
        string Message,
        string? FileName,
        int? Line,
        int? Column);

    private sealed record PublicFailureEnvelope(string Name, string Message);

    private sealed class DockerUnavailableException(string message) : Exception(message);
}
