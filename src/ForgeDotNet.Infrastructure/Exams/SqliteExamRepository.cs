using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Exams;

public sealed class SqliteExamRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IExamRepository, IExamAccessPolicy
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public async ValueTask<ExamAttempt?> GetAsync(
        Guid profileId,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, attemptId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ExamAttemptRecord? record = await context.ExamAttempts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProfileId == profileId && item.Id == attemptId, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async ValueTask<ExamAttempt?> GetActiveAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ExamAttemptRecord? record = await context.ExamAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.Status == ExamAttemptStatus.Active)
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async ValueTask<IReadOnlyList<ExamAttempt>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ExamAttemptRecord[] records = await context.ExamAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderByDescending(item => item.StartedAtUtc)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(ToDomain).ToArray());
    }

    public async ValueTask<ExamAttempt> CreateAsync(
        ExamAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ExamRules.ValidateAttempt(attempt);
        if (attempt.Status != ExamAttemptStatus.Active || attempt.Version != 1)
        {
            throw new InvalidDataException("Une nouvelle tentative doit être active en version 1.");
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await context.ExamAttempts.AnyAsync(
            item => item.ProfileId == attempt.ProfileId && item.Status == ExamAttemptStatus.Active,
            cancellationToken))
        {
            throw new InvalidOperationException("Un examen actif existe déjà pour ce profil.");
        }

        context.ExamAttempts.Add(ToRecord(attempt));
        await context.SaveChangesAsync(cancellationToken);
        return attempt;
    }

    public async ValueTask<IReadOnlyList<ExamSubmission>> ListSubmissionsAsync(
        Guid profileId,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, attemptId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await context.ExamAttempts.AnyAsync(
            item => item.ProfileId == profileId && item.Id == attemptId,
            cancellationToken))
        {
            throw new KeyNotFoundException("La tentative d’examen n’existe pas.");
        }

        ExamSubmissionRecord[] records = await context.ExamSubmissions.AsNoTracking()
            .Where(item => item.AttemptId == attemptId)
            .OrderBy(item => item.ItemId)
            .ThenBy(item => item.Sequence)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(ToDomain).ToArray());
    }

    public async ValueTask<ExamAttempt> SaveSubmissionAsync(
        Guid profileId,
        int expectedVersion,
        ExamAttempt updatedAttempt,
        ExamSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        ExamRules.ValidateAttempt(updatedAttempt);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ExamAttemptRecord current = await context.ExamAttempts.SingleOrDefaultAsync(
            item => item.ProfileId == profileId && item.Id == updatedAttempt.Id,
            cancellationToken)
            ?? throw new KeyNotFoundException("La tentative d’examen n’existe pas.");
        ExamAttempt currentDomain = ToDomain(current);
        if (current.Status != ExamAttemptStatus.Active
            || current.Version != expectedVersion
            || updatedAttempt.Version != expectedVersion + 1
            || !SameImmutableState(currentDomain, updatedAttempt)
            || submission.AttemptId != current.Id)
        {
            throw new InvalidOperationException("La soumission concurrente ou incohérente est refusée.");
        }

        current.Version = updatedAttempt.Version;
        context.ExamSubmissions.Add(ToRecord(submission));
        await context.SaveChangesAsync(cancellationToken);
        return updatedAttempt;
    }

    public async ValueTask<ExamCompletion> SaveCompletionAsync(
        Guid profileId,
        int expectedVersion,
        ExamCompletion completion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        ExamRules.ValidateAttempt(completion.Attempt);
        if (completion.Attempt.Status == ExamAttemptStatus.Active
            || completion.Attempt.Version != expectedVersion + 1
            || completion.Report.AttemptId != completion.Attempt.Id
            || completion.Report.Status != completion.Attempt.Status)
        {
            throw new InvalidDataException("La clôture d’examen est incohérente.");
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ExamAttemptRecord current = await context.ExamAttempts.SingleOrDefaultAsync(
            item => item.ProfileId == profileId && item.Id == completion.Attempt.Id,
            cancellationToken)
            ?? throw new KeyNotFoundException("La tentative d’examen n’existe pas.");
        if (current.Status != ExamAttemptStatus.Active || current.Version != expectedVersion)
        {
            throw new InvalidOperationException("L’examen a déjà été clôturé ou modifié.");
        }

        if (!SameImmutableState(ToDomain(current), completion.Attempt))
        {
            throw new InvalidDataException("La clôture tente de modifier le tirage figé.");
        }

        current.Status = completion.Attempt.Status;
        current.Version = completion.Attempt.Version;
        current.EndedAtUtc = completion.Attempt.EndedAtUtc;
        current.AssistanceDeclared = completion.Attempt.AssistanceDeclared;
        current.CompletionReason = completion.Attempt.CompletionReason;
        current.ReportJson = Serialize(completion.Report, 262_144, "rapport d’examen");
        await context.SaveChangesAsync(cancellationToken);
        return completion;
    }

    public async ValueTask<ExamReport?> GetReportAsync(
        Guid profileId,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, attemptId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ExamAttemptRecord? record = await context.ExamAttempts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProfileId == profileId && item.Id == attemptId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.Status == ExamAttemptStatus.Active)
        {
            if (record.ReportJson is not null)
            {
                throw new InvalidDataException("Un examen actif ne peut pas posséder de rapport.");
            }

            return null;
        }

        return Deserialize<ExamReport>(record.ReportJson, "Le rapport d’examen persisté est absent ou illisible.");
    }

    public async ValueTask<bool> IsLearningAidLockedAsync(
        Guid profileId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        if (nowUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("L’instant de verrouillage doit être en UTC.", nameof(nowUtc));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ExamAttempts.AsNoTracking().AnyAsync(
            item => item.ProfileId == profileId
                && item.Status == ExamAttemptStatus.Active
                && item.DeadlineUtc > nowUtc,
            cancellationToken);
    }

    private static ExamAttemptRecord ToRecord(ExamAttempt attempt) => new()
    {
        Id = attempt.Id,
        ProfileId = attempt.ProfileId,
        ExamId = attempt.ExamId,
        ExamVersion = attempt.ExamVersion,
        ExamRevision = attempt.ExamRevision,
        Title = attempt.Title,
        DurationMinutes = attempt.DurationMinutes,
        PassingScore = attempt.PassingScore,
        DrawAlgorithm = attempt.DrawAlgorithm,
        DrawSeed = attempt.DrawSeed,
        DrawCommitment = attempt.DrawCommitment,
        FrozenItemsJson = Serialize(attempt.Items, 262_144, "tirage d’examen"),
        Status = attempt.Status,
        Version = attempt.Version,
        StartedAtUtc = attempt.StartedAtUtc,
        DeadlineUtc = attempt.DeadlineUtc,
        EndedAtUtc = attempt.EndedAtUtc,
        AssistanceDeclared = attempt.AssistanceDeclared,
        CompletionReason = attempt.CompletionReason,
        ReportJson = null,
    };

    private static ExamAttempt ToDomain(ExamAttemptRecord record)
    {
        IReadOnlyList<ExamItemSnapshot> items = Deserialize<ExamItemSnapshot[]>(
            record.FrozenItemsJson,
            "Le tirage d’examen persisté est illisible.");
        var attempt = new ExamAttempt(
            record.Id,
            record.ProfileId,
            record.ExamId,
            record.ExamVersion,
            record.ExamRevision,
            record.Title,
            record.DurationMinutes,
            record.PassingScore,
            record.DrawAlgorithm,
            record.DrawSeed,
            record.DrawCommitment,
            items,
            record.Status,
            record.Version,
            record.StartedAtUtc,
            record.DeadlineUtc,
            record.EndedAtUtc,
            record.AssistanceDeclared,
            record.CompletionReason);
        ExamRules.ValidateAttempt(attempt);
        if ((record.Status == ExamAttemptStatus.Active) != (record.ReportJson is null))
        {
            throw new InvalidDataException("Le rapport et l’état de l’examen sont incohérents.");
        }

        return attempt;
    }

    private static ExamSubmissionRecord ToRecord(ExamSubmission submission) => new()
    {
        Id = submission.Id,
        AttemptId = submission.AttemptId,
        ItemId = submission.ItemId,
        Sequence = submission.Sequence,
        SourceFingerprint = submission.SourceFingerprint,
        SourceCode = submission.SourceCode,
        Outcome = submission.Outcome,
        TotalTests = submission.TotalTests,
        PassedTests = submission.PassedTests,
        HiddenFailureCount = submission.HiddenFailureCount,
        DiagnosticId = submission.DiagnosticId,
        SubmittedAtUtc = submission.SubmittedAtUtc,
    };

    private static ExamSubmission ToDomain(ExamSubmissionRecord record) => new(
        record.Id,
        record.AttemptId,
        record.ItemId,
        record.Sequence,
        record.SourceFingerprint,
        record.SourceCode,
        record.Outcome,
        record.TotalTests,
        record.PassedTests,
        record.HiddenFailureCount,
        record.DiagnosticId,
        record.SubmittedAtUtc);

    private static bool SameImmutableState(ExamAttempt left, ExamAttempt right) =>
        left.Id == right.Id
        && left.ProfileId == right.ProfileId
        && string.Equals(left.ExamId, right.ExamId, StringComparison.Ordinal)
        && left.ExamVersion == right.ExamVersion
        && string.Equals(left.ExamRevision, right.ExamRevision, StringComparison.Ordinal)
        && string.Equals(left.DrawAlgorithm, right.DrawAlgorithm, StringComparison.Ordinal)
        && string.Equals(left.DrawSeed, right.DrawSeed, StringComparison.Ordinal)
        && string.Equals(left.DrawCommitment, right.DrawCommitment, StringComparison.Ordinal)
        && left.StartedAtUtc == right.StartedAtUtc
        && left.DeadlineUtc == right.DeadlineUtc
        && JsonSerializer.Serialize(left.Items, SerializerOptions)
            == JsonSerializer.Serialize(right.Items, SerializerOptions);

    private static string Serialize<T>(T value, int maximumLength, string label)
    {
        string json = JsonSerializer.Serialize(value, SerializerOptions);
        return json.Length <= maximumLength
            ? json
            : throw new InvalidDataException($"Le {label} dépasse la taille autorisée.");
    }

    private static T Deserialize<T>(string? json, string message) =>
        string.IsNullOrWhiteSpace(json)
            ? throw new InvalidDataException(message)
            : JsonSerializer.Deserialize<T>(json, SerializerOptions)
                ?? throw new InvalidDataException(message);

    private static void ValidateKeys(Guid profileId, Guid attemptId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(attemptId, Guid.Empty);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
