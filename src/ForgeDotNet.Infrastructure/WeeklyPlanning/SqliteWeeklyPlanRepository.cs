using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.WeeklyPlanning;
using ForgeDotNet.Domain.WeeklyPlanning;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.WeeklyPlanning;

public sealed class SqliteWeeklyPlanRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IWeeklyPlanRepository
{
    private const int MaximumPlanJsonLength = 262_144;
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public async ValueTask<WeeklyPlanData?> GetLatestAsync(
        Guid profileId,
        Guid diagnosticSessionId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, diagnosticSessionId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        WeeklyPlanRecord? record = await context.WeeklyPlans
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.DiagnosticSessionId == diagnosticSessionId)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : ToData(record);
    }

    public async ValueTask<WeeklyPlanData> CreateInitialOrGetAsync(
        WeeklyPlanData plan,
        CancellationToken cancellationToken = default)
    {
        ValidateNewPlan(plan, expectedVersion: 1);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureEvaluationExistsAsync(context, plan, cancellationToken);
        WeeklyPlanRecord? existing = await context.WeeklyPlans
            .AsNoTracking()
            .Where(item => item.ProfileId == plan.ProfileId && item.DiagnosticSessionId == plan.DiagnosticSessionId)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return ToData(existing);
        }

        context.WeeklyPlans.Add(ToRecord(plan));
        await context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async ValueTask<WeeklyPlanData> CreateNextVersionAsync(
        WeeklyPlanData plan,
        int expectedPreviousVersion,
        CancellationToken cancellationToken = default)
    {
        ValidateNewPlan(plan, expectedPreviousVersion + 1);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        WeeklyPlanRecord current = await context.WeeklyPlans
            .Where(item => item.ProfileId == plan.ProfileId && item.DiagnosticSessionId == plan.DiagnosticSessionId)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Le plan à ajuster n'existe pas.");
        if (current.Version != expectedPreviousVersion || current.Status != WeeklyPlanStatus.Draft)
        {
            throw new InvalidOperationException("Le plan courant ne peut pas recevoir cette nouvelle version.");
        }

        WeeklyPlanData currentData = ToData(current);
        if (!SameSource(currentData.Snapshot, plan.Snapshot))
        {
            throw new InvalidOperationException("Un ajustement ne peut pas changer le diagnostic ou le curriculum figé.");
        }

        context.WeeklyPlans.Add(ToRecord(plan));
        await context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async ValueTask<WeeklyPlanData> AcceptAsync(
        Guid profileId,
        Guid diagnosticSessionId,
        int expectedVersion,
        DateTimeOffset acceptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, diagnosticSessionId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        if (acceptedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La date d'acceptation doit être exprimée en UTC.", nameof(acceptedAtUtc));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        WeeklyPlanRecord current = await context.WeeklyPlans
            .Where(item => item.ProfileId == profileId && item.DiagnosticSessionId == diagnosticSessionId)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Le plan à accepter n'existe pas.");
        if (current.Version != expectedVersion)
        {
            throw new InvalidOperationException("Le plan a changé ; rechargez la version courante avant de l'accepter.");
        }

        if (current.Status == WeeklyPlanStatus.Accepted)
        {
            return ToData(current);
        }

        current.Status = WeeklyPlanStatus.Accepted;
        current.AcceptedAtUtc = acceptedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        return ToData(current);
    }

    private static WeeklyPlanRecord ToRecord(WeeklyPlanData plan) => new()
    {
        Id = plan.Id,
        ProfileId = plan.ProfileId,
        DiagnosticSessionId = plan.DiagnosticSessionId,
        Version = plan.Version,
        Status = plan.Status,
        CurriculumId = plan.Snapshot.Curriculum.Id,
        CurriculumVersion = plan.Snapshot.Curriculum.Version,
        CurriculumRevision = plan.Snapshot.Curriculum.Revision,
        TargetWeeklyHours = plan.Snapshot.TargetWeeklyHours,
        PlanJson = Serialize(plan.Snapshot),
        CreatedAtUtc = plan.CreatedAtUtc,
        AcceptedAtUtc = plan.AcceptedAtUtc,
    };

    private static WeeklyPlanData ToData(WeeklyPlanRecord record)
    {
        WeeklyPlanSnapshot snapshot = JsonSerializer.Deserialize<WeeklyPlanSnapshot>(
            record.PlanJson,
            SerializerOptions)
            ?? throw new InvalidDataException("Le plan hebdomadaire persisté est illisible.");
        WeeklyPlanRules.ValidateSnapshot(snapshot);
        if (record.Id == Guid.Empty
            || record.ProfileId == Guid.Empty
            || record.DiagnosticSessionId != snapshot.DiagnosticSessionId
            || record.Version < 1
            || !string.Equals(record.CurriculumId, snapshot.Curriculum.Id, StringComparison.Ordinal)
            || record.CurriculumVersion != snapshot.Curriculum.Version
            || !string.Equals(record.CurriculumRevision, snapshot.Curriculum.Revision, StringComparison.Ordinal)
            || record.TargetWeeklyHours != snapshot.TargetWeeklyHours
            || record.CreatedAtUtc.Offset != TimeSpan.Zero
            || (record.Status == WeeklyPlanStatus.Accepted) != (record.AcceptedAtUtc is not null))
        {
            throw new InvalidDataException("Les métadonnées du plan ne correspondent pas à son snapshot.");
        }

        return new WeeklyPlanData(
            record.Id,
            record.ProfileId,
            record.DiagnosticSessionId,
            record.Version,
            record.Status,
            snapshot,
            record.CreatedAtUtc,
            record.AcceptedAtUtc);
    }

    private static void ValidateNewPlan(WeeklyPlanData plan, int expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ValidateKeys(plan.ProfileId, plan.DiagnosticSessionId);
        WeeklyPlanRules.ValidateSnapshot(plan.Snapshot);
        if (plan.Id == Guid.Empty
            || plan.Version != expectedVersion
            || plan.Status != WeeklyPlanStatus.Draft
            || plan.AcceptedAtUtc is not null
            || plan.CreatedAtUtc.Offset != TimeSpan.Zero
            || plan.Snapshot.DiagnosticSessionId != plan.DiagnosticSessionId)
        {
            throw new InvalidDataException("La nouvelle version du plan est invalide.");
        }
    }

    private static async Task EnsureEvaluationExistsAsync(
        ForgeDbContext context,
        WeeklyPlanData plan,
        CancellationToken cancellationToken)
    {
        bool exists = await context.DiagnosticSessions.AnyAsync(
            session => session.Id == plan.DiagnosticSessionId && session.ProfileId == plan.ProfileId,
            cancellationToken)
            && await context.DiagnosticEvaluations.AnyAsync(
                evaluation => evaluation.SessionId == plan.DiagnosticSessionId,
                cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Une évaluation persistée est requise avant le plan.");
        }
    }

    private static bool SameSource(WeeklyPlanSnapshot left, WeeklyPlanSnapshot right) =>
        left.DiagnosticSessionId == right.DiagnosticSessionId
        && string.Equals(left.EvaluationRubricId, right.EvaluationRubricId, StringComparison.Ordinal)
        && left.EvaluationRubricVersion == right.EvaluationRubricVersion
        && string.Equals(left.EvaluationRubricRevision, right.EvaluationRubricRevision, StringComparison.Ordinal)
        && string.Equals(left.Curriculum.Id, right.Curriculum.Id, StringComparison.Ordinal)
        && left.Curriculum.Version == right.Curriculum.Version
        && string.Equals(left.Curriculum.Revision, right.Curriculum.Revision, StringComparison.Ordinal)
        && left.Recommendations.SequenceEqual(right.Recommendations);

    private static string Serialize(WeeklyPlanSnapshot snapshot)
    {
        string json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        if (json.Length > MaximumPlanJsonLength)
        {
            throw new InvalidDataException("Le plan hebdomadaire dépasse la taille autorisée.");
        }

        return json;
    }

    private static void ValidateKeys(Guid profileId, Guid diagnosticSessionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(diagnosticSessionId, Guid.Empty);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
