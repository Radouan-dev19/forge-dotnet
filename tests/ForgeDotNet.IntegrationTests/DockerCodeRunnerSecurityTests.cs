using System.Diagnostics;
using System.Text.Json;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.CodeRunner;
using Xunit.Sdk;

namespace ForgeDotNet.IntegrationTests;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class DockerCodeRunnerSecurityTestGroup : ICollectionFixture<DockerSecurityFixture>
{
    public const string CollectionName = "CodeRunnerSecurityDocker";
}

[Collection(DockerCodeRunnerSecurityTestGroup.CollectionName)]
[Trait("Category", "CodeRunnerSecurity")]
public sealed class DockerCodeRunnerSecurityTests(DockerSecurityFixture fixture)
{
    [Fact]
    public async Task NormalProgramCompilesAndPassesVisibleAndHiddenTests()
    {
        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(SuccessSource));

        Assert.True(
            result.Status == CodeRunStatus.Succeeded,
            $"{result.Status}: {result.Summary} | {result.Compilation.Output.Text} | {result.Tests.Output.Text}");
        Assert.Equal(CodeRunStageStatus.Succeeded, result.Compilation.Status);
        Assert.Equal(CodeRunStageStatus.Succeeded, result.Tests.Status);
        Assert.Equal(2, result.Tests.PassedCount);
        Assert.Equal(0, result.Tests.HiddenFailureCount);
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task CompilationFailurePreventsTestsAndReturnsOnlySubmittedFileDiagnostics()
    {
        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(
            "public static class Submission { public static int Visible() => ; }"));

        Assert.Equal(CodeRunStatus.CompilationFailed, result.Status);
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
        Assert.NotEmpty(result.Compilation.Diagnostics);
        Assert.All(result.Compilation.Diagnostics, diagnostic =>
            Assert.True(diagnostic.FileName is null or "Submission.cs"));
        Assert.DoesNotContain("/workspace", result.Compilation.Output.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/input", result.Compilation.Output.Text, StringComparison.OrdinalIgnoreCase);
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task VisibleFailureIsPublicAndHiddenFailureIsRedacted()
    {
        CodeRunResult visible = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(Source("0", "7")));
        CodeRunResult hidden = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(Source("42", "0")));

        Assert.Equal(CodeRunStatus.TestsFailed, visible.Status);
        Assert.Single(visible.Tests.VisibleFailures);
        Assert.Equal(0, visible.Tests.HiddenFailureCount);
        Assert.Equal(CodeRunStatus.TestsFailed, hidden.Status);
        Assert.Empty(hidden.Tests.VisibleFailures);
        Assert.Equal(1, hidden.Tests.HiddenFailureCount);
        Assert.True(hidden.Tests.HiddenFailuresRedacted);
        Assert.DoesNotContain("Hidden", hidden.Tests.Output.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/workspace", hidden.Tests.Output.Text, StringComparison.OrdinalIgnoreCase);
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task InfiniteLoopIsKilledByTestTimeout()
    {
        string source = "public static class Submission { public static int Visible() => 42; public static int Hidden() { while (true) { } } }";
        var stopwatch = Stopwatch.StartNew();

        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(source));

        stopwatch.Stop();
        Assert.Equal(CodeRunStatus.TimedOut, result.Status);
        Assert.Equal(CodeRunStageStatus.Succeeded, result.Compilation.Status);
        Assert.Equal(CodeRunStageStatus.TimedOut, result.Tests.Status);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(60));
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task MemoryBombIsContainedByCgroupLimit()
    {
        string source = "public static class Submission { private static readonly System.Collections.Generic.List<byte[]> Data = []; public static int Visible() => 42; public static int Hidden() { while (true) { Data.Add(new byte[16 * 1024 * 1024]); } } }";

        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(source));

        Assert.NotEqual(CodeRunStatus.Succeeded, result.Status);
        Assert.Contains(result.Status, new[] { CodeRunStatus.TimedOut, CodeRunStatus.TestsFailed, CodeRunStatus.Unavailable });
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task OutputBombIsBoundedAndMarkedAsTruncated()
    {
        string source = "public static class Submission { public static int Visible() => 42; public static int Hidden() { for (var i = 0; i < 70000; i++) { System.Console.Write(\"X\"); } return 7; } }";

        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(source));

        Assert.Equal(CodeRunStatus.Succeeded, result.Status);
        Assert.True(result.Tests.Output.IsTruncated);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(result.Tests.Output.Text) <= CodeRunContract.MaximumOutputBytes);
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task DiskBombCannotExceedTmpfsQuota()
    {
        string source = "public static class Submission { public static int Visible() => 42; public static int Hidden() { try { System.IO.File.WriteAllBytes(\"/workspace/disk-bomb.bin\", new byte[80 * 1024 * 1024]); return 0; } catch (System.IO.IOException) { return 7; } } }";

        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(source));

        Assert.Equal(CodeRunStatus.Succeeded, result.Status);
        await fixture.AssertNoArtifactsAsync();
    }

    [Theory]
    [MemberData(nameof(IsolationAbuseSources))]
    public async Task NetworkHostEnvironmentAndSubprocessAbusesAreBlocked(string source)
    {
        const string secretName = "FORGE_DOTNET_HOST_SECRET";
        string? previous = Environment.GetEnvironmentVariable(secretName);
        Environment.SetEnvironmentVariable(secretName, "must-not-enter-container");
        try
        {
            CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(source));

            Assert.Equal(CodeRunStatus.Succeeded, result.Status);
            Assert.DoesNotContain("must-not-enter-container", result.Compilation.Output.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("must-not-enter-container", result.Tests.Output.Text, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, previous);
        }

        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task TraversalIsRejectedBeforeDockerAndCreatesNoArtifact()
    {
        CodeRunRequest request = DockerSecurityFixture.CreateRequest(SuccessSource) with
        {
            SourceFiles = Array.AsReadOnly([
                new CodeRunSourceFile("../Submission.cs", SuccessSource),
            ]),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Runner.RunAsync(request).AsTask());
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task CancellationKillsAndRemovesContainer()
    {
        string source = "public static class Submission { public static int Visible() => 42; public static int Hidden() { while (true) { } } }";
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(source), cancellation.Token);

        Assert.Equal(CodeRunStatus.Cancelled, result.Status);
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task EffectiveContainerPolicyMatchesEveryRequiredControl()
    {
        string source = "public static class Submission { public static int Visible() => 42; public static int Hidden() { while (true) { } } }";
        CodeRunRequest request = DockerSecurityFixture.CreateRequest(source);
        using var cancellation = new CancellationTokenSource();
        Task<CodeRunResult> execution = fixture.Runner.RunAsync(request, cancellation.Token).AsTask();
        string containerName = await fixture.WaitForContainerAsync(request.RequestId, TimeSpan.FromSeconds(15));

        DockerTestCommand inspect = await fixture.DockerAsync(["inspect", containerName]);
        using JsonDocument document = JsonDocument.Parse(inspect.StandardOutput);
        JsonElement container = document.RootElement[0];
        JsonElement host = container.GetProperty("HostConfig");
        JsonElement config = container.GetProperty("Config");
        JsonElement mounts = container.GetProperty("Mounts");

        Assert.False(host.GetProperty("Privileged").GetBoolean());
        Assert.True(host.GetProperty("ReadonlyRootfs").GetBoolean());
        Assert.Equal("none", host.GetProperty("NetworkMode").GetString());
        Assert.Equal("none", host.GetProperty("IpcMode").GetString());
        Assert.Equal(512L * DockerCodeRunnerOptions.Mebibyte, host.GetProperty("Memory").GetInt64());
        Assert.Equal(512L * DockerCodeRunnerOptions.Mebibyte, host.GetProperty("MemorySwap").GetInt64());
        Assert.Equal(1_000_000_000, host.GetProperty("NanoCpus").GetInt64());
        Assert.Equal(64, host.GetProperty("PidsLimit").GetInt64());
        JsonElement noFileLimit = Assert.Single(
            host.GetProperty("Ulimits").EnumerateArray(),
            limit => limit.GetProperty("Name").GetString() == "nofile");
        Assert.Equal(256, noFileLimit.GetProperty("Soft").GetInt64());
        Assert.Equal(256, noFileLimit.GetProperty("Hard").GetInt64());
        Assert.DoesNotContain(
            host.GetProperty("Ulimits").EnumerateArray(),
            limit => limit.GetProperty("Name").GetString() == "fsize");
        Assert.True(host.GetProperty("Init").GetBoolean());
        Assert.Equal("none", host.GetProperty("LogConfig").GetProperty("Type").GetString());
        Assert.Contains("ALL", host.GetProperty("CapDrop").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains(
            host.GetProperty("SecurityOpt").EnumerateArray().Select(value => value.GetString()),
            value => value?.StartsWith("no-new-privileges", StringComparison.Ordinal) is true);
        Assert.Contains(
            host.GetProperty("SecurityOpt").EnumerateArray().Select(value => value.GetString()),
            value => string.Equals(value, "seccomp=builtin", StringComparison.Ordinal));
        Assert.Empty(host.GetProperty("Devices").EnumerateArray());
        Assert.Equal(DockerCodeRunnerOptions.RequiredContainerUser, config.GetProperty("User").GetString());
        Assert.DoesNotContain(
            config.GetProperty("Env").EnumerateArray().Select(value => value.GetString()),
            value => value?.StartsWith("FORGE_DOTNET_HOST_SECRET=", StringComparison.Ordinal) is true);
        JsonElement bind = Assert.Single(mounts.EnumerateArray(), mount => mount.GetProperty("Destination").GetString() == "/input");
        Assert.Equal("bind", bind.GetProperty("Type").GetString());
        Assert.False(bind.GetProperty("RW").GetBoolean());
        Assert.StartsWith(fixture.WorkspaceRoot, bind.GetProperty("Source").GetString(), StringComparison.OrdinalIgnoreCase);
        JsonElement tmpfs = host.GetProperty("Tmpfs");
        Assert.True(tmpfs.TryGetProperty("/workspace", out JsonElement workspaceTmpfs));
        Assert.Contains("size=67108864", workspaceTmpfs.GetString(), StringComparison.Ordinal);
        Assert.True(tmpfs.TryGetProperty("/tmp", out _));
        Assert.False(config.TryGetProperty("ExposedPorts", out JsonElement exposed)
            && exposed.ValueKind == JsonValueKind.Object
            && exposed.EnumerateObject().Any());

        cancellation.Cancel();
        CodeRunResult result = await execution;
        Assert.Equal(CodeRunStatus.Cancelled, result.Status);
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task GlobalConcurrencyLimitAllowsOnlyOneContainer()
    {
        using var runner = fixture.CreateRunner(maximumConcurrency: 1);
        string source = "public static class Submission { public static int Visible() => 42; public static int Hidden() { while (true) { } } }";
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        Task<CodeRunResult> first = runner.RunAsync(DockerSecurityFixture.CreateRequest(source), firstCancellation.Token).AsTask();
        Task<CodeRunResult> second = runner.RunAsync(DockerSecurityFixture.CreateRequest(source), secondCancellation.Token).AsTask();

        _ = await fixture.WaitForAnyRunnerContainerAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(1, await fixture.CountRunnerContainersAsync());

        firstCancellation.Cancel();
        secondCancellation.Cancel();
        CodeRunResult[] results = await Task.WhenAll(first, second);
        Assert.All(results, result => Assert.Equal(CodeRunStatus.Cancelled, result.Status));
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task OrphanedContainerAndWorkspaceAreRecoveredBeforeRun()
    {
        string orphanName = $"forge-dotnet-runner-{Guid.NewGuid():N}";
        DockerTestCommand create = await fixture.DockerAsync([
            "create",
            "--name", orphanName,
            "--label", $"{DockerCodeRunnerOptions.RunnerLabel}={DockerCodeRunnerOptions.RunnerLabelValue}",
            fixture.ImageReference,
        ]);
        Assert.Equal(0, create.ExitCode);
        string orphanWorkspace = Path.Combine(fixture.WorkspaceRoot, $"run-orphan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(orphanWorkspace);
        await File.WriteAllTextAsync(Path.Combine(orphanWorkspace, "orphan.txt"), "orphan");

        CodeRunResult result = await fixture.Runner.RunAsync(DockerSecurityFixture.CreateRequest(SuccessSource));

        Assert.Equal(CodeRunStatus.Succeeded, result.Status);
        DockerTestCommand inspect = await fixture.DockerAsync(["inspect", orphanName], requireSuccess: false);
        Assert.NotEqual(0, inspect.ExitCode);
        Assert.False(Directory.Exists(orphanWorkspace));
        await fixture.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task UnavailableDockerContextFailsClosedBeforeWorkspaceCreation()
    {
        using DockerCodeRunner runner = fixture.CreateRunner(
            dockerContext: $"missing-{Guid.NewGuid():N}");

        CodeRunResult result = await runner.RunAsync(DockerSecurityFixture.CreateRequest(SuccessSource));

        Assert.Equal(CodeRunStatus.Unavailable, result.Status);
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
        Assert.DoesNotContain("validé", result.Summary, StringComparison.OrdinalIgnoreCase);
        await fixture.AssertNoArtifactsAsync();
    }

    public static IEnumerable<object[]> IsolationAbuseSources()
    {
        yield return ["public static class Submission { public static int Visible() => 42; public static int Hidden() { try { using var client = new System.Net.Sockets.TcpClient(); client.Connect(\"1.1.1.1\", 80); return client.Connected ? 0 : 7; } catch { return 7; } } }"];
        yield return ["public static class Submission { public static int Visible() => 42; public static int Hidden() => System.IO.File.Exists(\"/host-sentinel-forge-dotnet\") ? 0 : 7; }"];
        yield return ["public static class Submission { public static int Visible() => 42; public static int Hidden() => System.Environment.GetEnvironmentVariable(\"FORGE_DOTNET_HOST_SECRET\") is null ? 7 : 0; }"];
        yield return ["public static class Submission { public static int Visible() => 42; public static int Hidden() { try { using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(\"/bin/sh\") { UseShellExecute = false }); process?.Kill(); return 0; } catch { return 7; } } }"];
        yield return ["public static class Submission { [System.Runtime.InteropServices.DllImport(\"libc\")] private static extern int fork(); public static int Visible() => 42; public static int Hidden() => fork() < 0 ? 7 : 0; }"];
    }

    private const string SuccessSource =
        "public static class Submission { public static int Visible() => 42; public static int Hidden() => 7; }";

    private static string Source(string visible, string hidden) =>
        $"public static class Submission {{ public static int Visible() => {visible}; public static int Hidden() => {hidden}; }}";
}

public sealed class DockerSecurityFixture : IAsyncLifetime
{
    private const string ImageTag = "forge-dotnet-runner:test";
    private readonly List<DockerCodeRunner> _additionalRunners = [];

    public string DockerContext { get; } = "desktop-linux";

    public string ImageReference { get; private set; } = string.Empty;

    public string WorkspaceRoot { get; } = Path.Combine(
        Path.GetTempPath(),
        "ForgeDotNet.CodeRunnerSecurity",
        "runner-workspaces");

    public DockerCodeRunner Runner { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DockerTestCommand image = await DockerAsync(
            ["image", "inspect", ImageTag, "--format", "{{.Id}}"]);
        ImageReference = image.StandardOutput.Trim();
        if (!ImageReference.StartsWith("sha256:", StringComparison.Ordinal)
            || ImageReference.Length != 71)
        {
            throw new XunitException("L’image de test CodeRunner n’est pas identifiée par sha256.");
        }

        if (Directory.Exists(WorkspaceRoot))
        {
            Directory.Delete(WorkspaceRoot, recursive: true);
        }

        Directory.CreateDirectory(WorkspaceRoot);
        Runner = CreateRunner();
    }

    public async Task DisposeAsync()
    {
        Runner.Dispose();
        foreach (DockerCodeRunner runner in _additionalRunners)
        {
            runner.Dispose();
        }

        await AssertNoArtifactsAsync();
        if (Directory.Exists(WorkspaceRoot))
        {
            Directory.Delete(WorkspaceRoot, recursive: true);
        }

        string parent = Path.GetDirectoryName(WorkspaceRoot)!;
        if (Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any())
        {
            Directory.Delete(parent);
        }
    }

    public DockerCodeRunner CreateRunner(
        int maximumConcurrency = 2,
        string? dockerContext = null)
    {
        var runner = new DockerCodeRunner(
            new DockerCodeRunnerOptions
            {
                DockerContext = dockerContext ?? DockerContext,
                ImageReference = ImageReference,
                WorkspaceRootPath = WorkspaceRoot,
                MaximumConcurrency = maximumConcurrency,
            },
            new SecuritySpecificationSource(),
            TimeProvider.System);
        if (Runner is not null)
        {
            _additionalRunners.Add(runner);
        }

        return runner;
    }

    public static CodeRunRequest CreateRequest(string source) => new(
        Guid.NewGuid(),
        "runner-security-fixture",
        1,
        new string('A', 64),
        Array.AsReadOnly([new CodeRunSourceFile("Submission.cs", source)]));

    public async Task<string> WaitForContainerAsync(Guid requestId, TimeSpan timeout)
    {
        string expected = $"forge-dotnet-runner-{requestId:N}";
        using var timeoutSource = new CancellationTokenSource(timeout);
        while (!timeoutSource.IsCancellationRequested)
        {
            DockerTestCommand list = await DockerAsync([
                "ps",
                "--all",
                "--format", "{{.Names}}",
                "--filter", $"name=^{expected}$",
            ]);
            if (list.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(expected, StringComparer.Ordinal))
            {
                return expected;
            }

            await Task.Delay(100, timeoutSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new XunitException("Le conteneur runner attendu n’a pas été observé.");
    }

    public async Task<string> WaitForAnyRunnerContainerAsync(TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        while (!timeoutSource.IsCancellationRequested)
        {
            DockerTestCommand list = await ListRunnerContainersAsync();
            string? name = list.StandardOutput.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (name is not null)
            {
                return name;
            }

            await Task.Delay(100, timeoutSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new XunitException("Aucun conteneur runner n’a été observé.");
    }

    public async Task<int> CountRunnerContainersAsync()
    {
        DockerTestCommand list = await ListRunnerContainersAsync();
        return list.StandardOutput.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    public async Task AssertNoArtifactsAsync()
    {
        Assert.Equal(0, await CountRunnerContainersAsync());
        if (Directory.Exists(WorkspaceRoot))
        {
            Assert.Empty(Directory.EnumerateFileSystemEntries(WorkspaceRoot));
        }
    }

    public async Task<DockerTestCommand> DockerAsync(
        IReadOnlyList<string> arguments,
        bool requireSuccess = true)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--context");
        startInfo.ArgumentList.Add(DockerContext);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new XunitException("Le client Docker de test n’a pas démarré.");
        }

        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new XunitException("La commande Docker de test a dépassé 30 secondes.");
        }

        var result = new DockerTestCommand(process.ExitCode, await stdout, await stderr);
        if (requireSuccess && result.ExitCode != 0)
        {
            throw new XunitException($"La commande Docker de test a échoué : {result.StandardError}");
        }

        return result;
    }

    private Task<DockerTestCommand> ListRunnerContainersAsync() => DockerAsync([
        "ps",
        "--all",
        "--format", "{{.Names}}",
        "--filter", $"label={DockerCodeRunnerOptions.RunnerLabel}={DockerCodeRunnerOptions.RunnerLabelValue}",
    ]);

    private sealed class SecuritySpecificationSource : IDockerRunSpecificationSource
    {
        public ValueTask<DockerRunSpecification?> GetAsync(
            CodeRunRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<DockerRunSpecification?>(new("forge-security-fixture-v1"));
    }
}

public sealed record DockerTestCommand(int ExitCode, string StandardOutput, string StandardError);
