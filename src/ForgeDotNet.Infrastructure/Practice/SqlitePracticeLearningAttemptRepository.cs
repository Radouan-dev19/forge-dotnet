using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Practice;

public sealed class SqlitePracticeLearningAttemptRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IPracticeLearningAttemptRepository
{
    public async ValueTask AppendAsync(
        PracticeLearningAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        attempt.Validate();

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        PracticeLearningAttemptRecord[] existing = await context.PracticeLearningAttempts.AsNoTracking()
            .Where(item => item.Id == attempt.Id || item.DiagnosticId == attempt.DiagnosticId)
            .ToArrayAsync(cancellationToken);
        if (existing.Length > 0)
        {
            if (existing.Length == 1
                && existing[0].Id == attempt.Id
                && existing[0].DiagnosticId == attempt.DiagnosticId
                && existing[0].SubmissionFingerprint == attempt.SubmissionFingerprint
                && existing[0].Status == attempt.Status)
            {
                return;
            }

            throw new InvalidOperationException("Une tentative de rejeu d’observation C# a été refusée.");
        }

        context.PracticeLearningAttempts.Add(ToRecord(attempt));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<PracticeLearningAttempt>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("Le profil C# est obligatoire.", nameof(profileId));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        PracticeLearningAttemptRecord[] records = await context.PracticeLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderBy(item => item.ObservedAtUtc)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(ToDomain).ToArray());
    }

    private static PracticeLearningAttemptRecord ToRecord(PracticeLearningAttempt attempt) => new()
    {
        Id = attempt.Id,
        ProfileId = attempt.ProfileId,
        ExerciseId = attempt.ExerciseId,
        ExerciseVersion = attempt.ExerciseVersion,
        ContentRevision = attempt.ContentRevision,
        SubmissionFingerprint = attempt.SubmissionFingerprint,
        Status = attempt.Status,
        TotalTests = attempt.TotalTests,
        PassedTests = attempt.PassedTests,
        DiagnosticId = attempt.DiagnosticId,
        ObservedAtUtc = attempt.ObservedAtUtc,
    };

    private static PracticeLearningAttempt ToDomain(PracticeLearningAttemptRecord record) => new(
        record.Id,
        record.ProfileId,
        record.ExerciseId,
        record.ExerciseVersion,
        record.ContentRevision,
        record.SubmissionFingerprint,
        record.Status,
        record.TotalTests,
        record.PassedTests,
        record.DiagnosticId,
        record.ObservedAtUtc);
}
