using ForgeDotNet.Application.Projects;
using ForgeDotNet.Domain.Projects;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Projects;

/// <summary>
/// Journal des soumissions de projet : on ajoute, on ne réécrit jamais.
/// </summary>
/// <remarks>
/// Une soumission rejouée sous la même identité avec un contenu différent est refusée. C'est la même
/// règle que pour les observations de pratique : sans elle, une preuve pourrait être réécrite après
/// coup pour ouvrir une porte.
/// </remarks>
public sealed class SqliteProjectSubmissionRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IProjectSubmissionRepository
{
    public async ValueTask AppendAsync(
        ProjectSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        submission.Validate();

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ProjectSubmissionRecord? existing = await context.ProjectSubmissions
            .FirstOrDefaultAsync(item => item.Id == submission.Id, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.SubmissionFingerprint, submission.SubmissionFingerprint, StringComparison.Ordinal)
                || !string.Equals(existing.ProjectId, submission.ProjectId, StringComparison.Ordinal)
                || existing.Status != submission.Status)
            {
                throw new InvalidOperationException(
                    "Une soumission de projet déjà enregistrée ne peut pas être rejouée avec un autre contenu.");
            }

            return;
        }

        context.ProjectSubmissions.Add(new ProjectSubmissionRecord
        {
            Id = submission.Id,
            ProfileId = submission.ProfileId,
            ProjectId = submission.ProjectId,
            ProjectVersion = submission.ProjectVersion,
            ContentRevision = submission.ContentRevision,
            SubmissionFingerprint = submission.SubmissionFingerprint,
            Status = submission.Status,
            TotalSuites = submission.TotalSuites,
            PassedSuites = submission.PassedSuites,
            TotalTests = submission.TotalTests,
            PassedTests = submission.PassedTests,
            AutomaticallyVerified = submission.AutomaticallyVerified,
            ObservedAtUtc = submission.ObservedAtUtc,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<ProjectSubmission>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ProjectSubmissionRecord[] records = await context.ProjectSubmissions.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderByDescending(item => item.ObservedAtUtc)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(Map).ToArray());
    }

    private static ProjectSubmission Map(ProjectSubmissionRecord record) => new(
        record.Id,
        record.ProfileId,
        record.ProjectId,
        record.ProjectVersion,
        record.ContentRevision,
        record.SubmissionFingerprint,
        record.Status,
        record.TotalSuites,
        record.PassedSuites,
        record.TotalTests,
        record.PassedTests,
        record.AutomaticallyVerified,
        record.ObservedAtUtc);
}
