using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Application.CodeRunner;

public sealed record RunExerciseCommand(
    Guid RequestId,
    string ExerciseId,
    int ExerciseVersion,
    string ContentRevision,
    IReadOnlyList<CodeRunSourceFile> SourceFiles);

public sealed class RunExercise(
    ICodeRunner codeRunner,
    RunExerciseHistory history,
    TimeProvider timeProvider,
    IPracticeLearningAttemptRepository? attemptRepository = null,
    ILocalProfileRepository? profileRepository = null)
{
    public async ValueTask<CodeRunResult> ExecuteAsync(
        RunExerciseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = new CodeRunRequest(
            command.RequestId,
            command.ExerciseId,
            command.ExerciseVersion,
            command.ContentRevision,
            command.SourceFiles);
        CodeRunContract.ValidateRequest(request);

        CodeRunResult result;
        try
        {
            result = await codeRunner.RunAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            result = new CodeRunResult(
                request.RequestId,
                CodeRunStatus.Cancelled,
                new CodeCompilationResult(
                    CodeRunStageStatus.Cancelled,
                    Array.Empty<CodeRunnerDiagnostic>(),
                    new CodeRunTextOutput("Exécution annulée.", IsTruncated: false)),
                NotRunTests(),
                "La demande a été annulée ; aucune validation automatique n'a eu lieu.",
                Guid.NewGuid(),
                now,
                now);
        }

        CodeRunResult normalized = CodeRunContract.NormalizeResult(request, result);
        await PersistObservationAsync(command, normalized, cancellationToken);
        history.Record(command.ExerciseId, normalized);
        return normalized;
    }

    public IReadOnlyList<CodeRunResult> GetHistory(string exerciseId) => history.Get(exerciseId);

    private async ValueTask PersistObservationAsync(
        RunExerciseCommand command,
        CodeRunResult result,
        CancellationToken cancellationToken)
    {
        if (attemptRepository is null && profileRepository is null)
        {
            return;
        }

        if (attemptRepository is null || profileRepository is null)
        {
            throw new InvalidOperationException("La persistance des observations C# est incomplètement configurée.");
        }

        var profile = await profileRepository.GetAsync(cancellationToken.IsCancellationRequested
            ? CancellationToken.None
            : cancellationToken);
        var attempt = new PracticeLearningAttempt(
            command.RequestId,
            profile.LocalId,
            command.ExerciseId,
            command.ExerciseVersion,
            command.ContentRevision,
            Fingerprint(command.SourceFiles),
            MapStatus(result.Status),
            result.Tests.TotalCount,
            result.Tests.PassedCount,
            result.DiagnosticId,
            result.CompletedAtUtc);
        attempt.Validate();
        await attemptRepository.AppendAsync(
            attempt,
            cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken);
    }

    private static string Fingerprint(IReadOnlyList<CodeRunSourceFile> sourceFiles)
    {
        string canonical = JsonSerializer.Serialize(sourceFiles.Select(item => new
        {
            item.FileName,
            item.Content,
        }));
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";
    }

    private static PracticeLearningAttemptStatus MapStatus(CodeRunStatus status) => status switch
    {
        CodeRunStatus.Succeeded => PracticeLearningAttemptStatus.Succeeded,
        CodeRunStatus.CompilationFailed => PracticeLearningAttemptStatus.CompilationFailed,
        CodeRunStatus.TestsFailed => PracticeLearningAttemptStatus.TestsFailed,
        CodeRunStatus.TimedOut => PracticeLearningAttemptStatus.TimedOut,
        CodeRunStatus.Cancelled => PracticeLearningAttemptStatus.Cancelled,
        CodeRunStatus.Unavailable => PracticeLearningAttemptStatus.Unavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static CodeTestResult NotRunTests() => new(
        CodeRunStageStatus.NotRun,
        TotalCount: 0,
        PassedCount: 0,
        FailedCount: 0,
        HiddenFailureCount: 0,
        HiddenFailuresRedacted: false,
        Array.Empty<VisibleTestFailure>(),
        new CodeRunTextOutput(string.Empty, IsTruncated: false));
}

public sealed class RunExerciseHistory
{
    private const int MaximumEntriesPerExercise = 20;
    private readonly ConcurrentDictionary<string, HistoryBucket> _entries = new(StringComparer.Ordinal);

    public void Record(string exerciseId, CodeRunResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exerciseId);
        ArgumentNullException.ThrowIfNull(result);
        _entries.GetOrAdd(exerciseId, static _ => new HistoryBucket()).Record(result);
    }

    public IReadOnlyList<CodeRunResult> Get(string exerciseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exerciseId);
        return _entries.TryGetValue(exerciseId, out HistoryBucket? bucket)
            ? bucket.Get()
            : Array.Empty<CodeRunResult>();
    }

    private sealed class HistoryBucket
    {
        private readonly object _sync = new();
        private readonly Dictionary<Guid, CodeRunResult> _results = [];

        public void Record(CodeRunResult result)
        {
            lock (_sync)
            {
                _results.TryAdd(result.RequestId, result);
                if (_results.Count <= MaximumEntriesPerExercise)
                {
                    return;
                }

                Guid oldest = _results.Values
                    .OrderBy(item => item.CompletedAtUtc)
                    .ThenBy(item => item.RequestId)
                    .First()
                    .RequestId;
                _results.Remove(oldest);
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<CodeRunResult> Get()
        {
            lock (_sync)
            {
                return Array.AsReadOnly(_results.Values
                    .OrderByDescending(item => item.CompletedAtUtc)
                    .ThenByDescending(item => item.RequestId)
                    .ToArray());
            }
        }
    }
}
