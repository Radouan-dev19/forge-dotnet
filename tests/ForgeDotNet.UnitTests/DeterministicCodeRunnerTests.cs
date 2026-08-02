using System.Text;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.CodeRunner;

namespace ForgeDotNet.UnitTests;

public sealed class DeterministicCodeRunnerTests
{
    [Fact]
    public async Task SuccessfulScenarioSeparatesCompilationAndTests()
    {
        var runner = CreateRunner(DeterministicRunScenario.Successful);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        Assert.Equal(CodeRunStatus.Succeeded, result.Status);
        Assert.Equal(CodeRunStageStatus.Succeeded, result.Compilation.Status);
        Assert.Equal(CodeRunStageStatus.Succeeded, result.Tests.Status);
        Assert.Equal(4, result.Tests.PassedCount);
        Assert.Contains("simulation", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompilationFailurePreventsTestsFromRunning()
    {
        var runner = CreateRunner(DeterministicRunScenario.CompilationFailure);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        Assert.Equal(CodeRunStatus.CompilationFailed, result.Status);
        Assert.Equal(CodeRunStageStatus.Failed, result.Compilation.Status);
        Assert.Contains(result.Compilation.Diagnostics, diagnostic => diagnostic.Code == "CS1002");
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
        Assert.Equal(0, result.Tests.TotalCount);
    }

    [Fact]
    public async Task HiddenFailureExposesOnlyARedactedCount()
    {
        var runner = CreateRunner(DeterministicRunScenario.HiddenTestFailure);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        Assert.Equal(CodeRunStatus.TestsFailed, result.Status);
        Assert.True(result.Tests.HiddenFailuresRedacted);
        Assert.Equal(1, result.Tests.HiddenFailureCount);
        Assert.Empty(result.Tests.VisibleFailures);
        Assert.DoesNotContain("chemin", result.Tests.Output.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("source", result.Tests.Output.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VisibleFailureContainsOnlyPublicFailureDetails()
    {
        var runner = CreateRunner(DeterministicRunScenario.VisibleTestFailure);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        VisibleTestFailure failure = Assert.Single(result.Tests.VisibleFailures);
        Assert.Equal("CalculateTotal_ReturnsExpectedSum", failure.Name);
        Assert.False(result.Tests.HiddenFailuresRedacted);
        Assert.Equal(0, result.Tests.HiddenFailureCount);
    }

    [Theory]
    [InlineData(DeterministicRunScenario.TimedOut, CodeRunStatus.TimedOut, CodeRunStageStatus.TimedOut)]
    [InlineData(DeterministicRunScenario.Unavailable, CodeRunStatus.Unavailable, CodeRunStageStatus.Unavailable)]
    public async Task TerminalScenarioDoesNotRunTests(
        DeterministicRunScenario scenario,
        CodeRunStatus expectedStatus,
        CodeRunStageStatus expectedCompilationStatus)
    {
        var runner = CreateRunner(scenario);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedCompilationStatus, result.Compilation.Status);
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
        Assert.DoesNotContain("code validé", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancellationProducesExplicitCancelledResult()
    {
        var runner = CreateRunner(DeterministicRunScenario.WaitForCancellation);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        CodeRunResult result = await runner.RunAsync(CreateRequest(), cancellation.Token);

        Assert.Equal(CodeRunStatus.Cancelled, result.Status);
        Assert.Equal(CodeRunStageStatus.Cancelled, result.Compilation.Status);
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
    }

    [Fact]
    public async Task LargeOutputIsUtf8BoundedAndMarkedAsTruncated()
    {
        var runner = CreateRunner(DeterministicRunScenario.LargeOutput);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        Assert.True(result.Compilation.Output.IsTruncated);
        Assert.True(Encoding.UTF8.GetByteCount(result.Compilation.Output.Text) <= CodeRunContract.MaximumOutputBytes);
        Assert.EndsWith("[… sortie tronquée …]", result.Compilation.Output.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConcurrentDuplicateRequestRunsScenarioOnlyOnce()
    {
        var options = new DeterministicCodeRunnerOptions
        {
            Scenarios = Array.AsReadOnly([DeterministicRunScenario.Successful]),
            Delay = TimeSpan.FromMilliseconds(100),
        };
        var runner = new DeterministicCodeRunner(options, TimeProvider.System);
        CodeRunRequest request = CreateRequest();

        Task<CodeRunResult> first = runner.RunAsync(request).AsTask();
        Task<CodeRunResult> second = runner.RunAsync(request).AsTask();
        CodeRunResult[] results = await Task.WhenAll(first, second);

        Assert.Same(results[0], results[1]);
        Assert.Equal(1, runner.InvocationCount);
    }

    [Fact]
    public async Task RequestIdCannotBeReusedWithDifferentSubmission()
    {
        var runner = CreateRunner(DeterministicRunScenario.Successful);
        CodeRunRequest original = CreateRequest();
        _ = await runner.RunAsync(original);
        CodeRunRequest changed = original with
        {
            SourceFiles = Array.AsReadOnly([new CodeRunSourceFile("Submission.cs", "return 42m;")]),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(changed).AsTask());
    }

    [Fact]
    public void ContractRejectsPathsDuplicateNamesAndOversizedSource()
    {
        CodeRunRequest request = CreateRequest();
        Assert.Throws<ArgumentException>(() => CodeRunContract.ValidateRequest(request with
        {
            SourceFiles = Array.AsReadOnly([new CodeRunSourceFile("../Submission.cs", "return 0m;")]),
        }));
        Assert.Throws<ArgumentException>(() => CodeRunContract.ValidateRequest(request with
        {
            SourceFiles = Array.AsReadOnly([
                new CodeRunSourceFile("Submission.cs", "return 0m;"),
                new CodeRunSourceFile("submission.cs", "return 1m;"),
            ]),
        }));
        Assert.Throws<ArgumentException>(() => CodeRunContract.ValidateRequest(request with
        {
            SourceFiles = Array.AsReadOnly([
                new CodeRunSourceFile("Submission.cs", new string('a', CodeRunContract.MaximumSourceFileBytes + 1)),
            ]),
        }));
    }

    [Fact]
    public void PublicRequestContractCannotCarryCommandsOrHiddenTests()
    {
        string[] propertyNames = typeof(CodeRunRequest).GetProperties().Select(property => property.Name).ToArray();

        Assert.Equal(
            ["RequestId", "ExerciseId", "ExerciseVersion", "ContentRevision", "SourceFiles"],
            propertyNames);
        Assert.DoesNotContain(propertyNames, name => name.Contains("Command", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Hidden", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScenarioSequenceIsStrictlyParsed()
    {
        IReadOnlyList<DeterministicRunScenario> scenarios =
            DeterministicCodeRunnerOptions.ParseScenarios("Successful,TimedOut,Unavailable");

        Assert.Equal(
            [
                DeterministicRunScenario.Successful,
                DeterministicRunScenario.TimedOut,
                DeterministicRunScenario.Unavailable,
            ],
            scenarios);
        Assert.Throws<InvalidDataException>(() =>
            DeterministicCodeRunnerOptions.ParseScenarios("Successful,ShellCommand"));
    }

    private static DeterministicCodeRunner CreateRunner(DeterministicRunScenario scenario) => new(
        new DeterministicCodeRunnerOptions { Scenarios = Array.AsReadOnly([scenario]) },
        TimeProvider.System);

    private static CodeRunRequest CreateRequest(Guid? requestId = null) => new(
        requestId ?? Guid.NewGuid(),
        "reference-total-001",
        ExerciseVersion: 1,
        ContentRevision: new string('A', 64),
        Array.AsReadOnly([
            new CodeRunSourceFile(
                "Submission.cs",
                "public static decimal CalculateTotal(IReadOnlyList<decimal> values) => values.Sum();"),
        ]));
}
