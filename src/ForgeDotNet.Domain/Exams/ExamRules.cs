using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeDotNet.Domain.Exams;

public static partial class ExamRules
{
    public const string DrawAlgorithm = "sha256-rank-v1";

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{2,99}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex("^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex RevisionPattern();

    public static ExamAttempt Start(
        Guid attemptId,
        Guid profileId,
        ExamBlueprint blueprint,
        ReadOnlySpan<byte> seed,
        DateTimeOffset startedAtUtc)
    {
        ValidateBlueprint(blueprint);
        if (attemptId == Guid.Empty || profileId == Guid.Empty)
        {
            throw new ArgumentException("Les identifiants de l’examen sont obligatoires.");
        }

        EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        if (seed.Length != 32)
        {
            throw new ArgumentException("Le seed de tirage doit contenir exactement 256 bits.", nameof(seed));
        }

        string seedHex = Convert.ToHexStringLower(seed);
        ExamItemSnapshot[] items = blueprint.Candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Rank = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
                    $"{DrawAlgorithm}|{seedHex}|{candidate.ItemId}|{candidate.ItemVersion}|{candidate.ContentRevision}"))),
            })
            .OrderBy(item => item.Rank, StringComparer.Ordinal)
            .ThenBy(item => item.Candidate.ItemId, StringComparer.Ordinal)
            .Take(blueprint.DrawCount)
            .Select((item, index) => new ExamItemSnapshot(
                index + 1,
                item.Candidate.ItemId,
                item.Candidate.ItemVersion,
                item.Candidate.ContentRevision,
                item.Candidate.Domain,
                item.Candidate.Title,
                item.Candidate.Statement,
                item.Candidate.Constraints,
                item.Candidate.StarterFileName,
                item.Candidate.StarterCode,
                item.Candidate.SubmissionKind))
            .ToArray();
        string commitment = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{DrawAlgorithm}|{seedHex}")));
        return new ExamAttempt(
            attemptId,
            profileId,
            blueprint.Id,
            blueprint.Version,
            blueprint.Revision,
            blueprint.Title,
            blueprint.DurationMinutes,
            blueprint.PassingScore,
            DrawAlgorithm,
            seedHex,
            commitment,
            Array.AsReadOnly(items),
            ExamAttemptStatus.Active,
            Version: 1,
            startedAtUtc,
            startedAtUtc.AddMinutes(blueprint.DurationMinutes),
            EndedAtUtc: null,
            AssistanceDeclared: false,
            CompletionReason: null);
    }

    public static bool CanResume(ExamAttempt attempt, DateTimeOffset nowUtc)
    {
        ValidateAttempt(attempt);
        EnsureUtc(nowUtc, nameof(nowUtc));
        return attempt.Status == ExamAttemptStatus.Active && nowUtc < attempt.DeadlineUtc;
    }

    public static ExamAttempt RecordSubmission(
        ExamAttempt attempt,
        int expectedVersion,
        DateTimeOffset submittedAtUtc)
    {
        ValidateAttempt(attempt);
        EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        EnsureMutable(attempt, expectedVersion, submittedAtUtc);
        return attempt with { Version = attempt.Version + 1 };
    }

    public static ExamCompletion Finish(
        ExamAttempt attempt,
        int expectedVersion,
        IReadOnlyList<ExamSubmission> submissions,
        ExamCompletionReason requestedReason,
        bool assistanceDeclared,
        DateTimeOffset endedAtUtc)
    {
        ValidateAttempt(attempt);
        EnsureUtc(endedAtUtc, nameof(endedAtUtc));
        if (attempt.Status != ExamAttemptStatus.Active || attempt.Version != expectedVersion)
        {
            throw new InvalidOperationException("L’examen est déjà terminé ou sa version a changé.");
        }

        if (endedAtUtc < attempt.StartedAtUtc)
        {
            throw new InvalidOperationException("La fin d’examen précède son début.");
        }

        ExamCompletionReason reason = endedAtUtc >= attempt.DeadlineUtc
            ? ExamCompletionReason.DeadlineReached
            : requestedReason;
        ExamAttemptStatus status = reason switch
        {
            ExamCompletionReason.Submitted => ExamAttemptStatus.Completed,
            ExamCompletionReason.Abandoned => ExamAttemptStatus.Abandoned,
            ExamCompletionReason.DeadlineReached => ExamAttemptStatus.TimedOut,
            _ => throw new ArgumentOutOfRangeException(nameof(requestedReason)),
        };
        ValidateSubmissions(attempt, submissions, endedAtUtc);
        ExamItemReport[] itemReports = attempt.Items.Select(item =>
        {
            ExamSubmission[] itemSubmissions = submissions
                .Where(candidate => string.Equals(candidate.ItemId, item.ItemId, StringComparison.Ordinal))
                .OrderBy(candidate => candidate.Sequence)
                .ToArray();
            ExamSubmission? latest = itemSubmissions.LastOrDefault();
            bool verified = latest is not null
                && latest.Outcome is ExamSubmissionOutcome.Succeeded
                    or ExamSubmissionOutcome.CompilationFailed
                    or ExamSubmissionOutcome.TestsFailed
                    or ExamSubmissionOutcome.TimedOut;
            decimal score = latest is not null && latest.TotalTests > 0
                ? Math.Round(100m * latest.PassedTests / latest.TotalTests, 2, MidpointRounding.AwayFromZero)
                : 0m;
            return new ExamItemReport(
                item.ItemId,
                item.Title,
                item.Domain,
                latest is not null,
                verified,
                latest?.Outcome,
                score,
                latest?.TotalTests ?? 0,
                latest?.PassedTests ?? 0,
                latest?.HiddenFailureCount ?? 0,
                itemSubmissions.Length);
        }).ToArray();
        decimal score = itemReports.Length == 0
            ? 0m
            : Math.Round(itemReports.Average(item => item.Score), 2, MidpointRounding.AwayFromZero);
        bool passed = status == ExamAttemptStatus.Completed
            && !assistanceDeclared
            && itemReports.All(item => item.IsAutomaticallyVerified)
            && score >= attempt.PassingScore;
        var report = new ExamReport(
            attempt.Id,
            status,
            reason,
            score,
            passed,
            assistanceDeclared,
            attempt.DrawAlgorithm,
            attempt.DrawSeed,
            attempt.DrawCommitment,
            attempt.StartedAtUtc,
            endedAtUtc,
            Array.AsReadOnly(itemReports));
        ExamAttempt updated = attempt with
        {
            Status = status,
            Version = attempt.Version + 1,
            EndedAtUtc = endedAtUtc,
            AssistanceDeclared = assistanceDeclared,
            CompletionReason = reason,
        };
        return new ExamCompletion(updated, report);
    }

    public static void ValidateBlueprint(ExamBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        if (!IdPattern().IsMatch(blueprint.Id ?? string.Empty)
            || blueprint.Version < 1
            || !RevisionPattern().IsMatch(blueprint.Revision ?? string.Empty)
            || string.IsNullOrWhiteSpace(blueprint.Title)
            || blueprint.Title.Length > 160
            || blueprint.DurationMinutes is < 5 or > 180
            || blueprint.DrawCount is < 1 or > 8
            || blueprint.PassingScore is < 0 or > 100
            || blueprint.Candidates.Count < blueprint.DrawCount
            || blueprint.Candidates.Count > 32
            || blueprint.Candidates.Select(item => item.ItemId).Distinct(StringComparer.Ordinal).Count()
                != blueprint.Candidates.Count)
        {
            throw new InvalidDataException("La banque d’examen est invalide.");
        }

        foreach (ExamCandidate candidate in blueprint.Candidates)
        {
            if (!IdPattern().IsMatch(candidate.ItemId ?? string.Empty)
                || candidate.ItemVersion < 1
                || !RevisionPattern().IsMatch(candidate.ContentRevision ?? string.Empty)
                || !Enum.IsDefined(candidate.Domain)
                || string.IsNullOrWhiteSpace(candidate.Title)
                || candidate.Title.Length > 160
                || string.IsNullOrWhiteSpace(candidate.Statement)
                || candidate.Statement.Length > 32_000
                || candidate.Constraints.Count > 20
                || candidate.Constraints.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1_000)
                || !Enum.IsDefined(candidate.SubmissionKind)
                || (candidate.SubmissionKind == ExamSubmissionKind.CSharp
                    && !string.Equals(candidate.StarterFileName, "Submission.cs", StringComparison.Ordinal))
                || (candidate.SubmissionKind == ExamSubmissionKind.Sql
                    && !string.Equals(candidate.StarterFileName, "Submission.sql", StringComparison.Ordinal))
                || string.IsNullOrWhiteSpace(candidate.StarterCode)
                || candidate.StarterCode.Length > 64_000)
            {
                throw new InvalidDataException("Un item de la banque d’examen est invalide.");
            }
        }
    }

    public static void ValidateAttempt(ExamAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        if (attempt.Id == Guid.Empty
            || attempt.ProfileId == Guid.Empty
            || !IdPattern().IsMatch(attempt.ExamId ?? string.Empty)
            || attempt.ExamVersion < 1
            || !RevisionPattern().IsMatch(attempt.ExamRevision ?? string.Empty)
            || !string.Equals(attempt.DrawAlgorithm, DrawAlgorithm, StringComparison.Ordinal)
            || attempt.DrawSeed.Length != 64
            || attempt.DrawCommitment.Length != 64
            || attempt.Items.Count is < 1 or > 8
            || attempt.Items.Select(item => item.ItemId).Distinct(StringComparer.Ordinal).Count() != attempt.Items.Count
            || attempt.Items.Select(item => item.Position).Order().SequenceEqual(Enumerable.Range(1, attempt.Items.Count)) is false
            || attempt.Items.Any(item => !IdPattern().IsMatch(item.ItemId ?? string.Empty)
                || item.ItemVersion < 1
                || !RevisionPattern().IsMatch(item.ContentRevision ?? string.Empty)
                || !Enum.IsDefined(item.Domain)
                || !Enum.IsDefined(item.SubmissionKind)
                || string.IsNullOrWhiteSpace(item.Title)
                || item.Title.Length > 160
                || string.IsNullOrWhiteSpace(item.Statement)
                || item.Statement.Length > 32_000
                || item.Constraints.Count > 20
                || item.Constraints.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1_000)
                || (item.SubmissionKind == ExamSubmissionKind.CSharp
                    && !string.Equals(item.StarterFileName, "Submission.cs", StringComparison.Ordinal))
                || (item.SubmissionKind == ExamSubmissionKind.Sql
                    && !string.Equals(item.StarterFileName, "Submission.sql", StringComparison.Ordinal))
                || string.IsNullOrWhiteSpace(item.StarterCode)
                || item.StarterCode.Length > 64_000)
            || attempt.Version < 1
            || attempt.StartedAtUtc.Offset != TimeSpan.Zero
            || attempt.DeadlineUtc.Offset != TimeSpan.Zero
            || attempt.DeadlineUtc != attempt.StartedAtUtc.AddMinutes(attempt.DurationMinutes)
            || (attempt.Status == ExamAttemptStatus.Active) != (attempt.EndedAtUtc is null)
            || (attempt.Status == ExamAttemptStatus.Active) != (attempt.CompletionReason is null))
        {
            throw new InvalidDataException("La tentative d’examen est incohérente.");
        }
    }

    private static void EnsureMutable(ExamAttempt attempt, int expectedVersion, DateTimeOffset nowUtc)
    {
        if (attempt.Status != ExamAttemptStatus.Active || attempt.Version != expectedVersion)
        {
            throw new InvalidOperationException("L’examen est terminé ou sa version a changé.");
        }

        if (nowUtc >= attempt.DeadlineUtc)
        {
            throw new InvalidOperationException("La durée serveur de l’examen est écoulée.");
        }
    }

    private static void ValidateSubmissions(
        ExamAttempt attempt,
        IReadOnlyList<ExamSubmission> submissions,
        DateTimeOffset endedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(submissions);
        if (submissions.Any(item => item.Id == Guid.Empty
                || item.AttemptId != attempt.Id
                || !attempt.Items.Any(examItem => string.Equals(examItem.ItemId, item.ItemId, StringComparison.Ordinal))
                || item.Sequence < 1
                || string.IsNullOrWhiteSpace(item.SourceFingerprint)
                || item.SourceFingerprint.Length > 80
                || string.IsNullOrWhiteSpace(item.SourceCode)
                || item.SourceCode.Length > 64_000
                || !Enum.IsDefined(item.Outcome)
                || item.TotalTests < 0
                || item.PassedTests < 0
                || item.PassedTests > item.TotalTests
                || item.HiddenFailureCount < 0
                || item.HiddenFailureCount > item.TotalTests - item.PassedTests
                || item.DiagnosticId == Guid.Empty
                || item.SubmittedAtUtc.Offset != TimeSpan.Zero
                || item.SubmittedAtUtc < attempt.StartedAtUtc
                || item.SubmittedAtUtc > endedAtUtc)
            || submissions.GroupBy(item => item.ItemId, StringComparer.Ordinal)
                .Any(group => !group.OrderBy(item => item.Sequence)
                    .Select(item => item.Sequence)
                    .SequenceEqual(Enumerable.Range(1, group.Count()))))
        {
            throw new InvalidDataException("Les soumissions de l’examen sont incohérentes.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("L’instant doit être exprimé en UTC.", parameterName);
        }
    }
}

