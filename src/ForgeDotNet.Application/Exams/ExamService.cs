using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Exams;

public sealed class ExamService(
    IExamBankSource bankSource,
    IExamRepository repository,
    ILocalProfileRepository profileRepository,
    ICodeRunner codeRunner,
    ISqlExamRunner sqlExamRunner,
    TimeProvider timeProvider)
{
    public async ValueTask<ExamHomeView> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await CloseExpiredActiveAttemptAsync(profile.LocalId, cancellationToken);
        IReadOnlyList<ExamBlueprint> blueprints = await bankSource.ListAsync(cancellationToken);
        ExamAttempt[] attempts = (await repository.ListAsync(profile.LocalId, cancellationToken))
            .OrderByDescending(item => item.StartedAtUtc)
            .ToArray();
        var reports = new Dictionary<Guid, ExamReport?>();
        foreach (ExamAttempt attempt in attempts.Where(item => item.Status != ExamAttemptStatus.Active))
        {
            reports[attempt.Id] = await repository.GetReportAsync(profile.LocalId, attempt.Id, cancellationToken);
        }

        ExamAttempt? active = attempts.FirstOrDefault(item => item.Status == ExamAttemptStatus.Active);
        return new ExamHomeView(
            Array.AsReadOnly(blueprints.Select(ToSummary).ToArray()),
            active is null ? null : ToSummary(active, report: null),
            Array.AsReadOnly(attempts
                .Where(item => item.Status != ExamAttemptStatus.Active)
                .Select(item => ToSummary(item, reports[item.Id]))
                .ToArray()));
    }

    public async ValueTask<ExamAttemptView> StartAsync(
        string examId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(examId) || examId.Length > 100)
        {
            throw new ArgumentException("L’identifiant d’examen est invalide.", nameof(examId));
        }

        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await CloseExpiredActiveAttemptAsync(profile.LocalId, cancellationToken);
        if (await repository.GetActiveAsync(profile.LocalId, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Un examen sans aide est déjà actif.");
        }

        ExamBlueprint blueprint = await bankSource.GetAsync(examId, cancellationToken)
            ?? throw new KeyNotFoundException("L’examen demandé n’existe pas dans la banque compatible.");
        byte[] seed = RandomNumberGenerator.GetBytes(32);
        ExamAttempt attempt = ExamRules.Start(
            Guid.NewGuid(),
            profile.LocalId,
            blueprint,
            seed,
            timeProvider.GetUtcNow());
        ExamAttempt stored = await repository.CreateAsync(attempt, cancellationToken);
        return await ToViewAsync(profile.LocalId, stored, cancellationToken);
    }

    public async ValueTask<ExamAttemptView> GetAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attemptId, Guid.Empty);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        ExamAttempt attempt = await repository.GetAsync(profile.LocalId, attemptId, cancellationToken)
            ?? throw new KeyNotFoundException("La tentative d’examen n’existe pas.");
        attempt = await CloseIfExpiredAsync(profile.LocalId, attempt, cancellationToken);
        return await ToViewAsync(profile.LocalId, attempt, cancellationToken);
    }

    public async ValueTask<ExamSubmissionReceiptView> SubmitAsync(
        Guid attemptId,
        int expectedVersion,
        string itemId,
        string sourceCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attemptId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        if (string.IsNullOrWhiteSpace(itemId) || itemId.Length > 100)
        {
            throw new ArgumentException("L’item d’examen est invalide.", nameof(itemId));
        }

        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        ExamAttempt attempt = await repository.GetAsync(profile.LocalId, attemptId, cancellationToken)
            ?? throw new KeyNotFoundException("La tentative d’examen n’existe pas.");
        DateTimeOffset beforeRun = timeProvider.GetUtcNow();
        if (!ExamRules.CanResume(attempt, beforeRun))
        {
            _ = await CloseIfExpiredAsync(profile.LocalId, attempt, cancellationToken);
            throw new InvalidOperationException("L’examen n’accepte plus de soumission.");
        }

        if (attempt.Version != expectedVersion)
        {
            throw new InvalidOperationException("L’examen a changé ; rechargez son état avant de soumettre.");
        }

        ExamItemSnapshot item = attempt.Items.SingleOrDefault(candidate =>
            string.Equals(candidate.ItemId, itemId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException("L’item ne fait pas partie du tirage figé.");
        ExamRunResult result = item.SubmissionKind switch
        {
            ExamSubmissionKind.CSharp => await RunCSharpAsync(item, sourceCode, cancellationToken),
            ExamSubmissionKind.Sql => await sqlExamRunner.RunAsync(item, sourceCode, cancellationToken),
            _ => throw new InvalidDataException("Le type de soumission de l’examen est invalide."),
        };
        DateTimeOffset submittedAtUtc = timeProvider.GetUtcNow();
        if (submittedAtUtc >= attempt.DeadlineUtc)
        {
            _ = await CompleteAsync(
                profile.LocalId,
                attempt,
                ExamCompletionReason.DeadlineReached,
                assistanceDeclared: false,
                cancellationToken);
            throw new InvalidOperationException("La durée serveur s’est écoulée pendant l’exécution ; le résultat n’est pas compté.");
        }

        IReadOnlyList<ExamSubmission> existing = await repository.ListSubmissionsAsync(
            profile.LocalId,
            attempt.Id,
            cancellationToken);
        int sequence = existing.Count(candidate => string.Equals(candidate.ItemId, item.ItemId, StringComparison.Ordinal)) + 1;
        var submission = new ExamSubmission(
            Guid.NewGuid(),
            attempt.Id,
            item.ItemId,
            sequence,
            Fingerprint(sourceCode),
            sourceCode,
            result.Outcome,
            result.TotalTests,
            result.PassedTests,
            result.HiddenFailureCount,
            result.DiagnosticId,
            submittedAtUtc);
        ExamAttempt updated = ExamRules.RecordSubmission(attempt, expectedVersion, submittedAtUtc);
        ExamAttempt stored = await repository.SaveSubmissionAsync(
            profile.LocalId,
            expectedVersion,
            updated,
            submission,
            cancellationToken);
        return new ExamSubmissionReceiptView(
            stored.Id,
            stored.Version,
            item.ItemId,
            sequence,
            submittedAtUtc,
            "Soumission enregistrée. Compilation, tests et détails restent masqués jusqu’à la fin.");
    }

    public async ValueTask<ExamAttemptView> FinishAsync(
        Guid attemptId,
        int expectedVersion,
        bool assistanceDeclared,
        CancellationToken cancellationToken = default) => await EndAsync(
            attemptId,
            expectedVersion,
            ExamCompletionReason.Submitted,
            assistanceDeclared,
            cancellationToken);

    public async ValueTask<ExamAttemptView> AbandonAsync(
        Guid attemptId,
        int expectedVersion,
        CancellationToken cancellationToken = default) => await EndAsync(
            attemptId,
            expectedVersion,
            ExamCompletionReason.Abandoned,
            assistanceDeclared: false,
            cancellationToken);

    private async ValueTask<ExamAttemptView> EndAsync(
        Guid attemptId,
        int expectedVersion,
        ExamCompletionReason reason,
        bool assistanceDeclared,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attemptId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        ExamAttempt attempt = await repository.GetAsync(profile.LocalId, attemptId, cancellationToken)
            ?? throw new KeyNotFoundException("La tentative d’examen n’existe pas.");
        if (attempt.Version != expectedVersion)
        {
            throw new InvalidOperationException("L’examen a changé ; rechargez son état avant de le terminer.");
        }

        ExamCompletion completion = await CompleteAsync(
            profile.LocalId,
            attempt,
            reason,
            assistanceDeclared,
            cancellationToken);
        return await ToViewAsync(profile.LocalId, completion.Attempt, cancellationToken);
    }

    private async ValueTask CloseExpiredActiveAttemptAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        ExamAttempt? active = await repository.GetActiveAsync(profileId, cancellationToken);
        if (active is not null)
        {
            _ = await CloseIfExpiredAsync(profileId, active, cancellationToken);
        }
    }

    private async ValueTask<ExamAttempt> CloseIfExpiredAsync(
        Guid profileId,
        ExamAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (attempt.Status != ExamAttemptStatus.Active || timeProvider.GetUtcNow() < attempt.DeadlineUtc)
        {
            return attempt;
        }

        return (await CompleteAsync(
            profileId,
            attempt,
            ExamCompletionReason.DeadlineReached,
            assistanceDeclared: false,
            cancellationToken)).Attempt;
    }

    private async ValueTask<ExamCompletion> CompleteAsync(
        Guid profileId,
        ExamAttempt attempt,
        ExamCompletionReason reason,
        bool assistanceDeclared,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExamSubmission> submissions = await repository.ListSubmissionsAsync(
            profileId,
            attempt.Id,
            cancellationToken);
        ExamCompletion completion = ExamRules.Finish(
            attempt,
            attempt.Version,
            submissions,
            reason,
            assistanceDeclared,
            timeProvider.GetUtcNow());
        return await repository.SaveCompletionAsync(
            profileId,
            attempt.Version,
            completion,
            cancellationToken);
    }

    private async ValueTask<ExamAttemptView> ToViewAsync(
        Guid profileId,
        ExamAttempt attempt,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExamSubmission> submissions = await repository.ListSubmissionsAsync(
            profileId,
            attempt.Id,
            cancellationToken);
        ExamReport? report = attempt.Status == ExamAttemptStatus.Active
            ? null
            : await repository.GetReportAsync(profileId, attempt.Id, cancellationToken)
                ?? throw new InvalidDataException("Le rapport d’un examen terminé est absent.");
        DateTimeOffset now = timeProvider.GetUtcNow();
        ExamItemView[] items = attempt.Items.Select(item =>
        {
            ExamSubmission[] itemSubmissions = submissions
                .Where(candidate => string.Equals(candidate.ItemId, item.ItemId, StringComparison.Ordinal))
                .OrderBy(candidate => candidate.Sequence)
                .ToArray();
            ExamSubmission? latest = itemSubmissions.LastOrDefault();
            return new ExamItemView(
                item.Position,
                item.ItemId,
                item.Title,
                item.Domain,
                item.Statement,
                item.Constraints,
                item.StarterFileName,
                latest?.SourceCode ?? item.StarterCode,
                latest is not null,
                itemSubmissions.Length,
                latest?.SubmittedAtUtc);
        }).ToArray();
        return new ExamAttemptView(
            attempt.Id,
            attempt.Version,
            attempt.Title,
            attempt.Status,
            StatusLabel(attempt.Status),
            attempt.DurationMinutes,
            attempt.Status == ExamAttemptStatus.Active
                ? Math.Max(0, (int)Math.Ceiling((attempt.DeadlineUtc - now).TotalSeconds))
                : 0,
            attempt.StartedAtUtc,
            attempt.DeadlineUtc,
            attempt.DrawAlgorithm,
            attempt.DrawCommitment,
            ExamRules.CanResume(attempt, now),
            attempt.Status == ExamAttemptStatus.Active && now < attempt.DeadlineUtc,
            Array.AsReadOnly(items),
            report is null ? null : ToReportView(report));
    }

    private static ExamSummaryView ToSummary(ExamBlueprint blueprint) => new(
        blueprint.Id,
        blueprint.Version,
        blueprint.Title,
        blueprint.DurationMinutes,
        blueprint.DrawCount,
        blueprint.Candidates.Count,
        blueprint.PassingScore);

    private static ExamAttemptSummaryView ToSummary(ExamAttempt attempt, ExamReport? report) => new(
        attempt.Id,
        attempt.Title,
        attempt.Status,
        StatusLabel(attempt.Status),
        report?.Score,
        report?.Passed,
        attempt.StartedAtUtc,
        attempt.EndedAtUtc);

    private static ExamReportView ToReportView(ExamReport report) => new(
        report.Status,
        StatusLabel(report.Status),
        report.Score,
        report.Passed,
        report.AssistanceDeclared,
        report.DrawAlgorithm,
        report.DrawSeed,
        report.DrawCommitment,
        report.StartedAtUtc,
        report.EndedAtUtc,
        Array.AsReadOnly(report.Items.Select(item => new ExamItemReportView(
            item.ItemId,
            item.Title,
            DomainLabel(item.Domain),
            item.WasSubmitted,
            item.IsAutomaticallyVerified,
            item.Outcome is null ? "Non soumis" : OutcomeLabel(item.Outcome.Value),
            item.Score,
            item.TotalTests,
            item.PassedTests,
            item.HiddenFailureCount,
            item.SubmissionCount)).ToArray()));

    private static string StatusLabel(ExamAttemptStatus status) => status switch
    {
        ExamAttemptStatus.Active => "En cours",
        ExamAttemptStatus.Completed => "Terminé",
        ExamAttemptStatus.Abandoned => "Abandonné",
        ExamAttemptStatus.TimedOut => "Temps écoulé",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static string OutcomeLabel(ExamSubmissionOutcome outcome) => outcome switch
    {
        ExamSubmissionOutcome.Succeeded => "Tests réussis",
        ExamSubmissionOutcome.CompilationFailed => "Compilation échouée",
        ExamSubmissionOutcome.TestsFailed => "Tests échoués",
        ExamSubmissionOutcome.TimedOut => "Délai runner dépassé",
        ExamSubmissionOutcome.Cancelled => "Exécution annulée",
        ExamSubmissionOutcome.Unavailable => "Validation automatique indisponible",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    private static ExamSubmissionOutcome MapOutcome(CodeRunStatus status) => status switch
    {
        CodeRunStatus.Succeeded => ExamSubmissionOutcome.Succeeded,
        CodeRunStatus.CompilationFailed => ExamSubmissionOutcome.CompilationFailed,
        CodeRunStatus.TestsFailed => ExamSubmissionOutcome.TestsFailed,
        CodeRunStatus.TimedOut => ExamSubmissionOutcome.TimedOut,
        CodeRunStatus.Cancelled => ExamSubmissionOutcome.Cancelled,
        CodeRunStatus.Unavailable => ExamSubmissionOutcome.Unavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private async ValueTask<ExamRunResult> RunCSharpAsync(
        ExamItemSnapshot item,
        string sourceCode,
        CancellationToken cancellationToken)
    {
        var request = new CodeRunRequest(
            Guid.NewGuid(),
            item.ItemId,
            item.ItemVersion,
            item.ContentRevision,
            [new CodeRunSourceFile(item.StarterFileName, sourceCode)]);
        CodeRunContract.ValidateRequest(request);
        CodeRunResult result = CodeRunContract.NormalizeResult(
            request,
            await codeRunner.RunAsync(request, cancellationToken));
        return new ExamRunResult(
            MapOutcome(result.Status),
            result.Tests.TotalCount,
            result.Tests.PassedCount,
            result.Tests.HiddenFailureCount,
            result.DiagnosticId);
    }

    private static string Fingerprint(string sourceCode) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sourceCode)))}";

    private static string DomainLabel(MasteryDomain domain) => domain switch
    {
        MasteryDomain.CSharp => "C#",
        MasteryDomain.Debugging => "Débogage",
        MasteryDomain.Sql => "SQL",
        MasteryDomain.Api => "API",
        MasteryDomain.Tests => "Tests",
        _ => domain.ToString(),
    };
}

