using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.SqlLab;

public sealed class SqliteSqlLearningAttemptRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : ISqlLearningAttemptRepository
{
    public async ValueTask AppendAsync(
        SqlLearningAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        if (attempt.Id == Guid.Empty || attempt.ProfileId == Guid.Empty || attempt.DiagnosticId == Guid.Empty)
        {
            throw new InvalidDataException("L’observation SQL à enregistrer est invalide.");
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        SqlLearningAttemptRecord? existing = await context.SqlLearningAttempts.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DiagnosticId == attempt.DiagnosticId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Id == attempt.Id && existing.QueryFingerprint == attempt.QueryFingerprint)
            {
                return;
            }

            throw new InvalidOperationException("Une tentative de rejeu d’observation SQL a été refusée.");
        }

        context.SqlLearningAttempts.Add(ToRecord(attempt));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<SqlLearningAttempt>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("Le profil SQL est obligatoire.", nameof(profileId));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        SqlLearningAttemptRecord[] records = await context.SqlLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderBy(item => item.ObservedAtUtc)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(ToDomain).ToArray());
    }

    private static SqlLearningAttemptRecord ToRecord(SqlLearningAttempt attempt) => new()
    {
        Id = attempt.Id,
        ProfileId = attempt.ProfileId,
        ScenarioId = attempt.ScenarioId,
        ScenarioVersion = attempt.ScenarioVersion,
        ContentRevision = attempt.ContentRevision,
        Status = attempt.Status,
        ValidationRequested = attempt.ValidationRequested,
        ValidationPassed = attempt.ValidationPassed,
        QueryFingerprint = attempt.QueryFingerprint,
        DiagnosticId = attempt.DiagnosticId,
        ObservedAtUtc = attempt.ObservedAtUtc,
        ElapsedMilliseconds = attempt.ElapsedMilliseconds,
    };

    private static SqlLearningAttempt ToDomain(SqlLearningAttemptRecord record) => new(
        record.Id,
        record.ProfileId,
        record.ScenarioId,
        record.ScenarioVersion,
        record.ContentRevision,
        record.Status,
        record.ValidationRequested,
        record.ValidationPassed,
        record.QueryFingerprint,
        record.DiagnosticId,
        record.ObservedAtUtc,
        record.ElapsedMilliseconds);
}
