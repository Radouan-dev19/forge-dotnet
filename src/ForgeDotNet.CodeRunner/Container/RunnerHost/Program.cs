using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ForgeDotNet.CodeRunner;

internal static partial class Program
{
    private const string InputRoot = "/input";
    private const string WorkspaceRoot = "/workspace";
    private const string SubmissionRoot = "/workspace/submission";
    private const string SubmissionAssembly = "/workspace/submission/Submission.dll";
    private const string CompilerAssembly = "/usr/share/dotnet/sdk/10.0.302/Roslyn/bincore/csc.dll";
    private const string ReferenceAssemblyRoot = "/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.10/ref/net10.0";
    private const string SecuritySuite = "forge-security-fixture-v1";
    private const string ChildResultPrefix = "FORGE_RESULT:";
    private const string CaseResultPrefix = "FORGE_CASE_RESULT:";
    private const string EncryptedSuitePath = "/input/suite.bin";
    private const int MaximumSourceFiles = 8;
    private const int MaximumSourceFileBytes = 64 * 1024;
    private const int MaximumTotalSourceBytes = 256 * 1024;
    private const int MaximumCapturedCharacters = 128 * 1024;
    private const string CompilationTimeoutVariable = "FORGE_RUNNER_COMPILATION_TIMEOUT_MS";
    private const string TestTimeoutVariable = "FORGE_RUNNER_TEST_TIMEOUT_MS";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.-]*\\.cs$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceFileNamePattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{2,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ApprovedSuiteIdPattern();

    [GeneratedRegex("^(?<file>[^\\r\\n(]+)\\((?<line>\\d+),(?<column>\\d+)\\): (?<severity>warning|error) (?<code>[A-Z]+\\d+): (?<message>.*?)(?: \\[.*\\])?$", RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex CompilerDiagnosticPattern();

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && string.Equals(args[0], "--execute-tests", StringComparison.Ordinal))
            {
                return ExecuteTests(args[1]);
            }

            if (args.Length == 1 && string.Equals(args[0], "--execute-case", StringComparison.Ordinal))
            {
                return await ExecuteCaseAsync();
            }

            if (args.Length != 0)
            {
                return 2;
            }

            return await RunAsync();
        }
        catch (Exception exception)
        {
            WriteMessage(RunnerMessage.Unavailable(
                $"Le runner isolé a refusé la demande (diagnostic {FailureCode(exception)})."));
            return 3;
        }
    }

    private static string FailureCode(Exception exception) => exception switch
    {
        RunnerStageException stage => $"{stage.Stage}-{FailureCode(stage.InnerException!)}",
        CryptographicException => "suite-cryptographique",
        JsonException => "suite-json",
        InvalidDataException => "données-invalides",
        IOException => "entree-sortie",
        UnauthorizedAccessException => "acces-refuse",
        OperationCanceledException => "annulation-interne",
        InvalidOperationException => "operation-invalide",
        ArgumentException => "argument-invalide",
        NotSupportedException => "operation-non-supportee",
        TypeLoadException => "chargement-type",
        NullReferenceException => "reference-nulle",
        _ => "interne-inattendu",
    };

    private static async Task<int> RunAsync()
    {
        TimeSpan compilationLimit = ReadBoundedTimeout(
            CompilationTimeoutVariable,
            defaultSeconds: 25,
            minimumSeconds: 2,
            maximumSeconds: 30);
        TimeSpan testLimit = ReadBoundedTimeout(
            TestTimeoutVariable,
            defaultSeconds: 30,
            minimumSeconds: 1,
            maximumSeconds: 30);
        ContainerRequest request = await RunStageAsync(
            "requete",
            ReadAndValidateRequestAsync);
        RunnerSuiteDefinition? suite = request.HasEncryptedSuite
            ? await RunStageAsync("suite", () => ReadEncryptedSuiteAsync(request))
            : null;
        RunStage("sources", () => PrepareSubmissionSources(request));

        using var compilationTimeout = new CancellationTokenSource(compilationLimit);
        ProcessResult compilation = await RunStageAsync(
            "compilation",
            () => RunProcessAsync(
                "dotnet",
                BuildCompilerArguments(request),
                SubmissionRoot,
                compilationTimeout.Token));
        string compilationOutput = SanitizeOutput(compilation.CombinedOutput);
        if (compilation.TimedOut)
        {
            WriteMessage(RunnerMessage.CompilationTimedOut(compilationOutput));
            return 4;
        }

        if (compilation.ExitCode != 0)
        {
            WriteMessage(RunnerMessage.CompilationFailed(
                ParseDiagnostics(compilationOutput, request.SourceFiles),
                compilationOutput,
                compilation.OutputTruncated));
            return 1;
        }

        WriteMessage(RunnerMessage.CompilationSucceeded(compilationOutput, compilation.OutputTruncated));

        using var testTimeout = new CancellationTokenSource(testLimit);
        if (suite is not null)
        {
            RunnerMessage dynamicResult = await RunStageAsync(
                "tests-contenu",
                () => ExecuteContentTestsAsync(suite, testTimeout.Token));
            WriteMessage(dynamicResult);
            return dynamicResult.Status == "succeeded" ? 0 : 1;
        }

        ProcessResult tests = await RunProcessAsync(
            "dotnet",
            ["/opt/forge-runner/ForgeDotNet.RunnerHost.dll", "--execute-tests", request.SuiteId],
            WorkspaceRoot,
            testTimeout.Token);
        if (tests.TimedOut)
        {
            WriteMessage(RunnerMessage.TestsTimedOut(SanitizeOutput(tests.CombinedOutput), tests.OutputTruncated));
            return 7;
        }

        string? childPayload = ExtractChildPayload(tests.TailOutput);
        if (tests.ExitCode != 0 || childPayload is null)
        {
            WriteMessage(RunnerMessage.TestsFailed(
                [new PublicFailure("Visible_Execution", "L’exécution isolée du test visible s’est interrompue.")],
                hiddenFailureCount: 0,
                SanitizeOutput(tests.CombinedOutput),
                tests.OutputTruncated));
            return 1;
        }

        ChildTestResult? child = JsonSerializer.Deserialize<ChildTestResult>(childPayload, JsonOptions);
        if (child is null || child.InfrastructureFailure)
        {
            WriteMessage(RunnerMessage.Unavailable("Le renforcement seccomp du processus de test n’a pas pu être appliqué."));
            return 8;
        }

        string testOutput = RemoveChildPayload(SanitizeOutput(tests.CombinedOutput));
        RunnerMessage result = child.VisibleFailures.Count == 0 && child.HiddenFailureCount == 0
            ? RunnerMessage.TestsSucceeded(testOutput, tests.OutputTruncated)
            : RunnerMessage.TestsFailed(child.VisibleFailures, child.HiddenFailureCount, testOutput, tests.OutputTruncated);
        WriteMessage(result);
        return result.Status == "succeeded" ? 0 : 1;
    }

    private static void RunStage(string stage, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            throw new RunnerStageException(stage, exception);
        }
    }

    private static async Task<T> RunStageAsync<T>(string stage, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            throw new RunnerStageException(stage, exception);
        }
    }

    private static TimeSpan ReadBoundedTimeout(
        string variableName,
        int defaultSeconds,
        int minimumSeconds,
        int maximumSeconds)
    {
        string? raw = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return TimeSpan.FromSeconds(defaultSeconds);
        }

        int minimumMilliseconds = checked(minimumSeconds * 1000);
        int maximumMilliseconds = checked(maximumSeconds * 1000);
        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int milliseconds)
            || milliseconds < minimumMilliseconds
            || milliseconds > maximumMilliseconds)
        {
            throw new InvalidDataException("Délai runner refusé.");
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static async Task<RunnerSuiteDefinition> ReadEncryptedSuiteAsync(ContainerRequest request)
    {
        byte[] envelope = await File.ReadAllBytesAsync(EncryptedSuitePath);
        if (envelope.Length is <= 28 or > 256 * 1024 + 28)
        {
            throw new InvalidDataException("Suite chiffrée invalide.");
        }

        string? encodedKey = await Console.In.ReadLineAsync();
        if (encodedKey is null || encodedKey.Length > 64)
        {
            throw new InvalidDataException("Clé éphémère absente.");
        }

        byte[] key = Convert.FromBase64String(encodedKey);
        if (key.Length != 32)
        {
            throw new InvalidDataException("Clé éphémère invalide.");
        }

        byte[] plaintext = new byte[envelope.Length - 28];
        try
        {
            try
            {
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(
                    envelope.AsSpan(0, 12),
                    envelope.AsSpan(28),
                    envelope.AsSpan(12, 16),
                    plaintext);
            }
            catch (Exception exception)
            {
                throw new RunnerStageException("dechiffrement", exception);
            }

            RunnerSuiteDefinition suite;
            try
            {
                suite = JsonSerializer.Deserialize<RunnerSuiteDefinition>(plaintext, JsonOptions)
                    ?? throw new InvalidDataException("Suite approuvée vide.");
            }
            catch (Exception exception)
            {
                throw new RunnerStageException("deserialisation", exception);
            }

            if (suite.SchemaVersion != 1
                || !string.Equals(suite.SuiteId, request.SuiteId, StringComparison.Ordinal)
                || suite.Cases.Count is < 2 or > 40)
            {
                throw new InvalidDataException("Suite approuvée incohérente.");
            }

            try
            {
                SeccompPolicy.DisableDumping();
            }
            catch (Exception exception)
            {
                throw new RunnerStageException("protection-memoire", exception);
            }

            return suite;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static async Task<RunnerMessage> ExecuteContentTestsAsync(
        RunnerSuiteDefinition suite,
        CancellationToken cancellationToken)
    {
        var visibleFailures = new List<PublicFailure>();
        int hiddenFailures = 0;
        bool truncated = false;
        var output = new StringBuilder();
        foreach (RunnerTestCase testCase in suite.Cases)
        {
            var invocation = new RunnerCaseInvocation(
                suite.TypeName,
                suite.MethodName,
                suite.ParameterTypes,
                suite.ReturnType,
                testCase.Arguments,
                testCase.ArgumentsUnchanged);
            ProcessResult process = await RunProcessAsync(
                "/usr/share/dotnet/dotnet",
                ["/opt/forge-runner/ForgeDotNet.RunnerHost.dll", "--execute-case"],
                WorkspaceRoot,
                cancellationToken,
                JsonSerializer.Serialize(invocation, JsonOptions),
                clearEnvironment: true);
            if (process.TimedOut)
            {
                return RunnerMessage.TestsTimedOut(SanitizeOutput(process.CombinedOutput), process.OutputTruncated);
            }

            truncated |= process.OutputTruncated;
            string publicOutput = RemoveCasePayload(SanitizeOutput(process.CombinedOutput));
            if (!string.IsNullOrWhiteSpace(publicOutput))
            {
                output.AppendLine(publicOutput);
            }

            string? payload = ExtractPayload(process.TailOutput, CaseResultPrefix);
            RunnerCaseResult? actual = payload is null
                ? null
                : JsonSerializer.Deserialize<RunnerCaseResult>(payload, JsonOptions);
            bool passed = process.ExitCode == 0
                && actual is not null
                && !actual.InfrastructureFailure
                && CasePassed(suite, testCase, actual);
            if (!passed)
            {
                if (testCase.IsVisible)
                {
                    visibleFailures.Add(new PublicFailure(testCase.Name, testCase.Message));
                }
                else
                {
                    hiddenFailures++;
                }
            }
        }

        string boundedOutput = Bound(output.ToString().Trim(), MaximumCapturedCharacters);
        return visibleFailures.Count == 0 && hiddenFailures == 0
            ? RunnerMessage.TestsSucceeded(suite.Cases.Count, boundedOutput, truncated)
            : RunnerMessage.TestsFailed(
                suite.Cases.Count,
                visibleFailures,
                hiddenFailures,
                boundedOutput,
                truncated);
    }

    private static bool CasePassed(
        RunnerSuiteDefinition suite,
        RunnerTestCase testCase,
        RunnerCaseResult actual)
    {
        if (testCase.ExpectedException is not null)
        {
            return string.Equals(actual.ExceptionType, testCase.ExpectedException, StringComparison.Ordinal);
        }

        if (actual.ExceptionType is not null || actual.ResultJson is null)
        {
            return false;
        }

        Type returnType = RunnerTypeCatalog.Resolve(suite.ReturnType);
        object? expected = testCase.Expected.Deserialize(returnType, JsonOptions);
        object? result = JsonSerializer.Deserialize(actual.ResultJson, returnType, JsonOptions);
        JsonNode? expectedNode = JsonNode.Parse(JsonSerializer.Serialize(expected, returnType, JsonOptions));
        JsonNode? resultNode = JsonNode.Parse(JsonSerializer.Serialize(result, returnType, JsonOptions));
        if (!JsonNode.DeepEquals(expectedNode, resultNode))
        {
            return false;
        }

        if (!testCase.ArgumentsUnchanged)
        {
            return true;
        }

        return actual.ArgumentsJson is not null
            && JsonNode.DeepEquals(JsonNode.Parse(testCase.Arguments.GetRawText()), JsonNode.Parse(actual.ArgumentsJson));
    }

    private static async Task<int> ExecuteCaseAsync()
    {
        try
        {
            SeccompPolicy.Apply();
            string json = await Console.In.ReadToEndAsync();
            if (Encoding.UTF8.GetByteCount(json) is <= 0 or > 64 * 1024)
            {
                throw new InvalidDataException("Invocation de cas invalide.");
            }

            RunnerCaseInvocation invocation = JsonSerializer.Deserialize<RunnerCaseInvocation>(json, JsonOptions)
                ?? throw new InvalidDataException("Invocation de cas vide.");
            Type[] parameterTypes = invocation.ParameterTypes.Select(RunnerTypeCatalog.Resolve).ToArray();
            Type returnType = RunnerTypeCatalog.Resolve(invocation.ReturnType);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(SubmissionAssembly);
            Type type = assembly.GetType(invocation.TypeName, throwOnError: true, ignoreCase: false)!;
            MethodInfo method = type.GetMethod(
                invocation.MethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: parameterTypes,
                modifiers: null) ?? throw new MissingMethodException();
            if (method.ReturnType != returnType)
            {
                throw new InvalidDataException("Type de retour incorrect.");
            }

            object?[] arguments = invocation.Arguments.EnumerateArray()
                .Select((item, index) => item.Deserialize(parameterTypes[index], JsonOptions))
                .ToArray();
            try
            {
                object? result = method.Invoke(null, arguments);
                WriteCaseResult(new RunnerCaseResult(
                    false,
                    JsonSerializer.Serialize(result, returnType, JsonOptions),
                    null,
                    invocation.CaptureArguments ? JsonSerializer.Serialize(arguments, JsonOptions) : null));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                WriteCaseResult(new RunnerCaseResult(
                    false,
                    null,
                    exception.InnerException.GetType().Name,
                    invocation.CaptureArguments ? JsonSerializer.Serialize(arguments, JsonOptions) : null));
            }

            return 0;
        }
        catch
        {
            WriteCaseResult(new RunnerCaseResult(true, null, null, null));
            return 11;
        }
    }

    private static int ExecuteTests(string suiteId)
    {
        if (!string.Equals(suiteId, SecuritySuite, StringComparison.Ordinal))
        {
            WriteChildResult(new ChildTestResult([], 0, InfrastructureFailure: true));
            return 9;
        }

        try
        {
            SeccompPolicy.Apply();
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(SubmissionAssembly);
            Type? submission = assembly.GetType("Submission", throwOnError: false, ignoreCase: false);
            var visibleFailures = new List<PublicFailure>();
            if (!InvokeInt32(submission, "Visible", expected: 42))
            {
                visibleFailures.Add(new PublicFailure(
                    "Visible_Returns42",
                    "Le comportement visible attendu n’est pas satisfait."));
            }

            int hiddenFailures = InvokeInt32(submission, "Hidden", expected: 7) ? 0 : 1;
            WriteChildResult(new ChildTestResult(visibleFailures, hiddenFailures, InfrastructureFailure: false));
            return 0;
        }
        catch
        {
            WriteChildResult(new ChildTestResult([], 0, InfrastructureFailure: true));
            return 10;
        }
    }

    private static bool InvokeInt32(Type? type, string methodName, int expected)
    {
        try
        {
            MethodInfo? method = type?.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            return method?.ReturnType == typeof(int) && method.Invoke(null, null) is int actual && actual == expected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<ContainerRequest> ReadAndValidateRequestAsync()
    {
        string requestPath = Path.Combine(InputRoot, "request.json");
        await using FileStream stream = new(requestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 or > 32 * 1024)
        {
            throw new InvalidDataException("Manifeste runner invalide.");
        }

        ContainerRequest? request = await JsonSerializer.DeserializeAsync<ContainerRequest>(stream, JsonOptions);
        bool securitySuite = request is not null
            && string.Equals(request.SuiteId, SecuritySuite, StringComparison.Ordinal)
            && !request.HasEncryptedSuite;
        bool contentSuite = request is not null
            && request.HasEncryptedSuite
            && ApprovedSuiteIdPattern().IsMatch(request.SuiteId ?? string.Empty)
            && !string.Equals(request.SuiteId, SecuritySuite, StringComparison.Ordinal)
            && File.Exists(EncryptedSuitePath);
        if (request is null
            || request.RequestId == Guid.Empty
            || (!securitySuite && !contentSuite)
            || request.SourceFiles is null
            || request.SourceFiles.Count is < 1 or > MaximumSourceFiles)
        {
            throw new InvalidDataException("Manifeste runner refusé.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalBytes = 0;
        foreach (string fileName in request.SourceFiles)
        {
            if (string.IsNullOrWhiteSpace(fileName)
                || fileName.Contains("..", StringComparison.Ordinal)
                || !SourceFileNamePattern().IsMatch(fileName)
                || !names.Add(fileName))
            {
                throw new InvalidDataException("Nom de source refusé.");
            }

            string sourcePath = Path.Combine(InputRoot, "sources", fileName);
            FileInfo info = new(sourcePath);
            if (!info.Exists || info.Length > MaximumSourceFileBytes)
            {
                throw new InvalidDataException("Source absente ou trop volumineuse.");
            }

            totalBytes = checked(totalBytes + (int)info.Length);
        }

        if (totalBytes > MaximumTotalSourceBytes)
        {
            throw new InvalidDataException("Volume source refusé.");
        }

        return request;
    }

    private static void PrepareSubmissionSources(ContainerRequest request)
    {
        Directory.CreateDirectory(SubmissionRoot);
        foreach (string fileName in request.SourceFiles)
        {
            string input = Path.Combine(InputRoot, "sources", fileName);
            string output = Path.Combine(SubmissionRoot, fileName);
            using FileStream source = new(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream destination = new(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(destination);
        }
    }

    private static List<string> BuildCompilerArguments(ContainerRequest request)
    {
        if (!File.Exists(CompilerAssembly) || !Directory.Exists(ReferenceAssemblyRoot))
        {
            throw new InvalidDataException("Chaîne de compilation absente de l’image épinglée.");
        }

        string[] referenceAssemblies = Directory.GetFiles(ReferenceAssemblyRoot, "*.dll", SearchOption.TopDirectoryOnly);
        if (referenceAssemblies.Length == 0)
        {
            throw new InvalidDataException("Assemblies de référence absentes de l’image épinglée.");
        }

        string[] approvedPackageAssemblies = Directory.GetFiles("/opt/forge-runner", "*.dll", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path).StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal)
                || Path.GetFileName(path).StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || Path.GetFileName(path).StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
                || Path.GetFileName(path).StartsWith("SQLitePCLRaw.", StringComparison.Ordinal))
            .ToArray();
        if (!approvedPackageAssemblies.Any(path =>
            string.Equals(Path.GetFileName(path), "Microsoft.EntityFrameworkCore.Sqlite.dll", StringComparison.Ordinal)))
        {
            throw new InvalidDataException("Assemblies EF Core approuvées absentes de l’image épinglée.");
        }

        var arguments = new List<string>(
            referenceAssemblies.Length + approvedPackageAssemblies.Length + request.SourceFiles.Count + 12)
        {
            CompilerAssembly,
            "/noconfig",
            "/nostdlib+",
            "/target:library",
            "/langversion:latest",
            "/nullable:enable",
            "/deterministic+",
            "/optimize+",
            "/warn:4",
            $"/out:{SubmissionAssembly}",
            $"/pathmap:{SubmissionRoot}=.",
        };
        foreach (string reference in referenceAssemblies.Order(StringComparer.Ordinal))
        {
            arguments.Add($"/reference:{reference}");
        }

        foreach (string reference in approvedPackageAssemblies.Order(StringComparer.Ordinal))
        {
            arguments.Add($"/reference:{reference}");
        }

        foreach (string fileName in request.SourceFiles.Order(StringComparer.Ordinal))
        {
            arguments.Add(Path.Combine(SubmissionRoot, fileName));
        }

        return arguments;
    }

    private static List<RunnerDiagnostic> ParseDiagnostics(
        string output,
        IReadOnlyList<string> sourceFiles)
    {
        var diagnostics = new List<RunnerDiagnostic>();
        foreach (Match match in CompilerDiagnosticPattern().Matches(output).Cast<Match>().Take(100))
        {
            string candidate = Path.GetFileName(match.Groups["file"].Value.Trim());
            string? fileName = sourceFiles.FirstOrDefault(source => string.Equals(source, candidate, StringComparison.OrdinalIgnoreCase));
            _ = int.TryParse(match.Groups["line"].Value, out int line);
            _ = int.TryParse(match.Groups["column"].Value, out int column);
            diagnostics.Add(new RunnerDiagnostic(
                match.Groups["code"].Value,
                match.Groups["severity"].Value,
                Bound(match.Groups["message"].Value, 2_000),
                fileName,
                line > 0 ? line : null,
                column > 0 ? column : null));
        }

        if (diagnostics.Count == 0)
        {
            diagnostics.Add(new RunnerDiagnostic(
                "CS0000",
                "error",
                "La compilation a échoué sans diagnostic public exploitable.",
                null,
                null,
                null));
        }

        return diagnostics;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken,
        string? standardInput = null,
        bool clearEnvironment = false)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
            CreateNoWindow = true,
        };
        if (clearEnvironment)
        {
            startInfo.Environment.Clear();
            startInfo.Environment["DOTNET_EnableDiagnostics"] = "0";
            startInfo.Environment["HOME"] = "/workspace/home";
        }
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Processus runner non démarré.");
        }

        Task<CapturedText> standardOutput = ReadBoundedAsync(process.StandardOutput, cancellationToken);
        Task<CapturedText> standardError = ReadBoundedAsync(process.StandardError, cancellationToken);
        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
        }
        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            await process.WaitForExitAsync(CancellationToken.None);
        }

        CapturedText stdout = await standardOutput;
        CapturedText stderr = await standardError;
        return new ProcessResult(
            timedOut ? -1 : process.ExitCode,
            stdout.Head,
            stderr.Head,
            stdout.Tail,
            stdout.Truncated || stderr.Truncated,
            timedOut);
    }

    private static async Task<CapturedText> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        char[] buffer = new char[4_096];
        var head = new StringBuilder();
        var tail = new StringBuilder();
        bool truncated = false;
        while (true)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(), CancellationToken.None);
            if (read == 0)
            {
                break;
            }

            if (head.Length < MaximumCapturedCharacters)
            {
                int accepted = Math.Min(read, MaximumCapturedCharacters - head.Length);
                head.Append(buffer, 0, accepted);
                truncated |= accepted < read;
            }
            else
            {
                truncated = true;
            }

            tail.Append(buffer, 0, read);
            if (tail.Length > MaximumCapturedCharacters)
            {
                tail.Remove(0, tail.Length - MaximumCapturedCharacters);
            }
        }

        return new CapturedText(head.ToString(), tail.ToString(), truncated);
    }

    private static string SanitizeOutput(string value) => Bound(
        value
            .Replace(SubmissionRoot + "/", string.Empty, StringComparison.Ordinal)
            .Replace(InputRoot + "/sources/", string.Empty, StringComparison.Ordinal)
            .Replace("/opt/forge-runner/", string.Empty, StringComparison.Ordinal),
        MaximumCapturedCharacters);

    private static string? ExtractChildPayload(string output) => ExtractPayload(output, ChildResultPrefix);

    private static string? ExtractPayload(string output, string prefix)
    {
        int index = output.LastIndexOf(prefix, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        int start = index + prefix.Length;
        int end = output.IndexOfAny(['\r', '\n'], start);
        return output[start..(end < 0 ? output.Length : end)];
    }

    private static string RemoveChildPayload(string output)
    {
        int index = output.LastIndexOf(ChildResultPrefix, StringComparison.Ordinal);
        return index < 0 ? output : output[..index].TrimEnd();
    }

    private static string RemoveCasePayload(string output)
    {
        int index = output.LastIndexOf(CaseResultPrefix, StringComparison.Ordinal);
        return index < 0 ? output : output[..index].TrimEnd();
    }

    private static void WriteMessage(RunnerMessage message)
    {
        Console.Out.WriteLine(JsonSerializer.Serialize(message, JsonOptions));
        Console.Out.Flush();
    }

    private static void WriteChildResult(ChildTestResult result)
    {
        Console.Out.WriteLine(ChildResultPrefix + JsonSerializer.Serialize(result, JsonOptions));
        Console.Out.Flush();
    }

    private static void WriteCaseResult(RunnerCaseResult result)
    {
        Console.Out.WriteLine(CaseResultPrefix + JsonSerializer.Serialize(result, JsonOptions));
        Console.Out.Flush();
    }

    private static string Bound(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record ContainerRequest(
        Guid RequestId,
        string SuiteId,
        IReadOnlyList<string> SourceFiles,
        bool HasEncryptedSuite);

    private sealed record RunnerDiagnostic(
        string Code,
        string Severity,
        string Message,
        string? FileName,
        int? Line,
        int? Column);

    private sealed record PublicFailure(string Name, string Message);

    private sealed record ChildTestResult(
        IReadOnlyList<PublicFailure> VisibleFailures,
        int HiddenFailureCount,
        bool InfrastructureFailure);

    private sealed record RunnerMessage(
        string Kind,
        string Status,
        string Stage,
        IReadOnlyList<RunnerDiagnostic> Diagnostics,
        string CompilationOutput,
        bool CompilationOutputTruncated,
        int TotalCount,
        int PassedCount,
        int FailedCount,
        int HiddenFailureCount,
        IReadOnlyList<PublicFailure> VisibleFailures,
        string TestOutput,
        bool TestOutputTruncated)
    {
        public static RunnerMessage CompilationSucceeded(string output, bool truncated) =>
            Empty("compilation", "succeeded", "compilation") with
            {
                CompilationOutput = output,
                CompilationOutputTruncated = truncated,
            };

        public static RunnerMessage CompilationFailed(
            IReadOnlyList<RunnerDiagnostic> diagnostics,
            string output,
            bool truncated) => Empty("result", "compilation-failed", "compilation") with
            {
                Diagnostics = diagnostics,
                CompilationOutput = output,
                CompilationOutputTruncated = truncated,
            };

        public static RunnerMessage CompilationTimedOut(string output) =>
            Empty("result", "timed-out", "compilation") with { CompilationOutput = output };

        public static RunnerMessage TestsSucceeded(string output, bool truncated) =>
            TestsSucceeded(2, output, truncated);

        public static RunnerMessage TestsSucceeded(int totalCount, string output, bool truncated) =>
            Empty("result", "succeeded", "tests") with
            {
                TotalCount = totalCount,
                PassedCount = totalCount,
                TestOutput = output,
                TestOutputTruncated = truncated,
            };

        public static RunnerMessage TestsFailed(
            IReadOnlyList<PublicFailure> failures,
            int hiddenFailureCount,
            string output,
            bool truncated) => TestsFailed(2, failures, hiddenFailureCount, output, truncated);

        public static RunnerMessage TestsFailed(
            int totalCount,
            IReadOnlyList<PublicFailure> failures,
            int hiddenFailureCount,
            string output,
            bool truncated)
        {
            int failed = failures.Count + hiddenFailureCount;
            return Empty("result", "tests-failed", "tests") with
            {
                TotalCount = totalCount,
                PassedCount = Math.Max(0, totalCount - failed),
                FailedCount = failed,
                HiddenFailureCount = hiddenFailureCount,
                VisibleFailures = failures,
                TestOutput = output,
                TestOutputTruncated = truncated,
            };
        }

        public static RunnerMessage TestsTimedOut(string output, bool truncated) =>
            Empty("result", "timed-out", "tests") with
            {
                TestOutput = output,
                TestOutputTruncated = truncated,
            };

        public static RunnerMessage Unavailable(string output) =>
            Empty("result", "unavailable", "compilation") with { CompilationOutput = output };

        private static RunnerMessage Empty(string kind, string status, string stage) => new(
            kind,
            status,
            stage,
            [],
            string.Empty,
            false,
            0,
            0,
            0,
            0,
            [],
            string.Empty,
            false);
    }

    private sealed record CapturedText(string Head, string Tail, bool Truncated);

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        string TailOutput,
        bool OutputTruncated,
        bool TimedOut)
    {
        public string CombinedOutput => string.Join('\n', new[] { StandardOutput, StandardError }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private sealed class RunnerStageException(string stage, Exception innerException)
        : Exception("Une étape interne du runner a échoué.", innerException)
    {
        public string Stage { get; } = stage;
    }

    private static class SeccompPolicy
    {
        private const uint ScmpActAllow = 0x7fff0000;
        private const uint ScmpActErrno = 0x00050001;
        private const int PrSetDumpable = 4;

        public static void DisableDumping()
        {
            if (Prctl(PrSetDumpable, 0, 0, 0, 0) != 0)
            {
                throw new InvalidOperationException("La mémoire du parent de tests n’a pas pu être protégée.");
            }
        }

        public static void Apply()
        {
            IntPtr context = SeccompInit(ScmpActAllow);
            if (context == IntPtr.Zero)
            {
                throw new InvalidOperationException("seccomp_init a échoué.");
            }

            try
            {
                foreach (string syscallName in new[] { "execve", "execveat", "fork", "vfork", "ptrace", "mount", "umount2", "unshare", "setns", "bpf", "keyctl", "open_by_handle_at" })
                {
                    int syscall = ResolveSyscall(syscallName);
                    if (syscall >= 0 && SeccompRuleAdd(context, ScmpActErrno, syscall, 0) != 0)
                    {
                        throw new InvalidOperationException("Une règle seccomp n’a pas pu être ajoutée.");
                    }
                }

                if (SeccompLoad(context) != 0)
                {
                    throw new InvalidOperationException("Le filtre seccomp n’a pas pu être chargé.");
                }
            }
            finally
            {
                SeccompRelease(context);
            }
        }

        [DllImport("libseccomp.so.2", EntryPoint = "seccomp_init")]
        private static extern IntPtr SeccompInit(uint defaultAction);

        private static int ResolveSyscall(string name) =>
            SeccompSyscallResolveName(Encoding.UTF8.GetBytes(name + '\0'));

        [DllImport("libseccomp.so.2", EntryPoint = "seccomp_syscall_resolve_name", ExactSpelling = true)]
        private static extern int SeccompSyscallResolveName([In] byte[] name);

        [DllImport("libseccomp.so.2", EntryPoint = "seccomp_rule_add")]
        private static extern int SeccompRuleAdd(IntPtr context, uint action, int syscall, uint argumentCount);

        [DllImport("libseccomp.so.2", EntryPoint = "seccomp_load")]
        private static extern int SeccompLoad(IntPtr context);

        [DllImport("libseccomp.so.2", EntryPoint = "seccomp_release")]
        private static extern void SeccompRelease(IntPtr context);

        [DllImport("libc", EntryPoint = "prctl", SetLastError = true)]
        private static extern int Prctl(int option, ulong argument2, ulong argument3, ulong argument4, ulong argument5);
    }
}
