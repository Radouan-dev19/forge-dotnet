using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.CodeRunner;

namespace ForgeDotNet.IntegrationTests;

public sealed class CodeRunnerOrchestrationTests
{
    [Fact]
    public async Task PracticeOrchestrationMapsScenarioSequenceAndKeepsBoundedVolatileHistory()
    {
        var options = new DeterministicCodeRunnerOptions
        {
            Scenarios = Array.AsReadOnly([
                DeterministicRunScenario.Successful,
                DeterministicRunScenario.CompilationFailure,
                DeterministicRunScenario.HiddenTestFailure,
                DeterministicRunScenario.TimedOut,
                DeterministicRunScenario.Unavailable,
            ]),
        };
        var doubleRunner = new DeterministicCodeRunner(options, TimeProvider.System);
        var history = new RunExerciseHistory();
        var runExercise = new RunExercise(doubleRunner, history, TimeProvider.System);

        foreach (Guid requestId in Enumerable.Range(0, options.Scenarios.Count).Select(_ => Guid.NewGuid()))
        {
            _ = await runExercise.ExecuteAsync(CreateCommand(requestId));
        }

        IReadOnlyList<CodeRunResult> results = runExercise.GetHistory("reference-total-001");
        Assert.Equal(5, results.Count);
        Assert.Contains(results, result => result.Status == CodeRunStatus.Succeeded);
        Assert.Contains(results, result => result.Status == CodeRunStatus.CompilationFailed
            && result.Tests.Status == CodeRunStageStatus.NotRun);
        CodeRunResult hiddenFailure = Assert.Single(results, result =>
            result.Tests.HiddenFailuresRedacted);
        Assert.Empty(hiddenFailure.Tests.VisibleFailures);
        Assert.Contains(results, result => result.Status == CodeRunStatus.TimedOut);
        CodeRunResult unavailable = Assert.Single(results, result => result.Status == CodeRunStatus.Unavailable);
        Assert.DoesNotContain("code validé", unavailable.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IdempotentRetryIsRecordedOnceAndInvokesDoubleOnce()
    {
        var doubleRunner = new DeterministicCodeRunner(
            new DeterministicCodeRunnerOptions
            {
                Scenarios = Array.AsReadOnly([DeterministicRunScenario.Successful]),
            },
            TimeProvider.System);
        var history = new RunExerciseHistory();
        var runExercise = new RunExercise(doubleRunner, history, TimeProvider.System);
        RunExerciseCommand command = CreateCommand(Guid.NewGuid());

        CodeRunResult first = await runExercise.ExecuteAsync(command);
        CodeRunResult second = await runExercise.ExecuteAsync(command);

        Assert.Equal(first.RequestId, second.RequestId);
        Assert.Equal(first.DiagnosticId, second.DiagnosticId);
        Assert.Equal(first.Status, second.Status);
        Assert.Single(runExercise.GetHistory(command.ExerciseId));
        Assert.Equal(1, doubleRunner.InvocationCount);
    }

    [Fact]
    public async Task CancellationIsReturnedAndAddedToHistoryWithoutRunningTests()
    {
        var doubleRunner = new DeterministicCodeRunner(
            new DeterministicCodeRunnerOptions
            {
                Scenarios = Array.AsReadOnly([DeterministicRunScenario.WaitForCancellation]),
            },
            TimeProvider.System);
        var runExercise = new RunExercise(doubleRunner, new RunExerciseHistory(), TimeProvider.System);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        CodeRunResult result = await runExercise.ExecuteAsync(CreateCommand(Guid.NewGuid()), cancellation.Token);

        Assert.Equal(CodeRunStatus.Cancelled, result.Status);
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
        Assert.Single(runExercise.GetHistory("reference-total-001"));
    }

    [Fact]
    public void ApplicationContractHasNoProcessOrDockerDependency()
    {
        string[] dependencies = typeof(ICodeRunner).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain("System.Diagnostics.Process", dependencies, StringComparer.Ordinal);
        Assert.DoesNotContain(dependencies, dependency => dependency.Contains("Docker", StringComparison.OrdinalIgnoreCase));
    }

    private static RunExerciseCommand CreateCommand(Guid requestId) => new(
        requestId,
        "reference-total-001",
        ExerciseVersion: 1,
        ContentRevision: new string('A', 64),
        Array.AsReadOnly([
            new CodeRunSourceFile(
                "Submission.cs",
                "public static decimal CalculateTotal(IReadOnlyList<decimal> values) => values.Sum();"),
        ]));
}
