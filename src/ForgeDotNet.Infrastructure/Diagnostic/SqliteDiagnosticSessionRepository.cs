using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Diagnostic;

public sealed class SqliteDiagnosticSessionRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IDiagnosticSessionRepository
{
    private const int MaximumPlanJsonLength = 131_072;
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public async ValueTask<DiagnosticSessionData?> GetAsync(
        Guid profileId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, sessionId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DiagnosticSessionRecord? record = await context.DiagnosticSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == sessionId && item.ProfileId == profileId,
                cancellationToken);
        return record is null ? null : await ToDataAsync(context, record, cancellationToken);
    }

    public async ValueTask<DiagnosticSessionData?> GetActiveAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DiagnosticSessionRecord? record = await context.DiagnosticSessions
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.Status == DiagnosticSessionStatus.Active)
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : await ToDataAsync(context, record, cancellationToken);
    }

    public async ValueTask<DiagnosticSessionData?> GetLatestAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DiagnosticSessionRecord? record = await context.DiagnosticSessions
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : await ToDataAsync(context, record, cancellationToken);
    }

    public async ValueTask<DiagnosticSessionData> CreateOrGetActiveAsync(
        DiagnosticSessionData session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateKeys(session.ProfileId, session.Id);
        string planJson = SerializePlan(session.Plan);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DiagnosticSessionRecord? active = await context.DiagnosticSessions
            .AsNoTracking()
            .Where(item => item.ProfileId == session.ProfileId && item.Status == DiagnosticSessionStatus.Active)
            .OrderByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (active is not null)
        {
            return await ToDataAsync(context, active, cancellationToken);
        }

        context.DiagnosticSessions.Add(new DiagnosticSessionRecord
        {
            Id = session.Id,
            ProfileId = session.ProfileId,
            BankId = session.BankId,
            BankVersion = session.BankVersion,
            BankRevision = session.BankRevision,
            Mode = session.Plan.Mode,
            Seed = session.Plan.Seed,
            Status = session.Timeline.SessionStatus,
            CurrentSectionIndex = session.Timeline.CurrentSectionIndex,
            SectionStatusesJson = SerializeStatuses(session.Timeline.SectionStatuses),
            FrozenPlanJson = planJson,
            SectionDurationSeconds = session.SectionDurationSeconds,
            StartedAtUtc = session.StartedAtUtc,
            UpdatedAtUtc = session.UpdatedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            SectionStartedAtUtc = session.Timeline.SectionStartedAtUtc,
            SectionDeadlineUtc = session.Timeline.SectionDeadlineUtc,
        });
        await context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async ValueTask SaveTimelineAsync(
        Guid profileId,
        Guid sessionId,
        DiagnosticTimeline timeline,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? endedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, sessionId);
        ArgumentNullException.ThrowIfNull(timeline);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DiagnosticSessionRecord record = await context.DiagnosticSessions.SingleOrDefaultAsync(
            item => item.Id == sessionId && item.ProfileId == profileId,
            cancellationToken)
            ?? throw new KeyNotFoundException("La session de diagnostic n'existe pas.");
        record.Status = timeline.SessionStatus;
        record.CurrentSectionIndex = timeline.CurrentSectionIndex;
        record.SectionStatusesJson = SerializeStatuses(timeline.SectionStatuses);
        record.SectionStartedAtUtc = timeline.SectionStartedAtUtc;
        record.SectionDeadlineUtc = timeline.SectionDeadlineUtc;
        record.UpdatedAtUtc = updatedAtUtc;
        record.EndedAtUtc = endedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask UpsertResponseAsync(
        Guid profileId,
        Guid sessionId,
        DiagnosticResponseData response,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, sessionId);
        ArgumentNullException.ThrowIfNull(response);
        ValidateResponse(response);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        bool sessionExists = await context.DiagnosticSessions.AnyAsync(
            item => item.Id == sessionId && item.ProfileId == profileId,
            cancellationToken);
        if (!sessionExists)
        {
            throw new KeyNotFoundException("La session de diagnostic n'existe pas.");
        }

        DiagnosticResponseRecord? record = await context.DiagnosticResponses.SingleOrDefaultAsync(
            item => item.SessionId == sessionId && item.QuestionId == response.QuestionId,
            cancellationToken);
        if (record is null)
        {
            context.DiagnosticResponses.Add(new DiagnosticResponseRecord
            {
                SessionId = sessionId,
                QuestionId = response.QuestionId,
                SelectedOptionId = response.SelectedOptionId,
                SavedAtUtc = response.SavedAtUtc,
            });
        }
        else
        {
            record.SelectedOptionId = response.SelectedOptionId;
            record.SavedAtUtc = response.SavedAtUtc;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<DiagnosticSessionData> ToDataAsync(
        ForgeDbContext context,
        DiagnosticSessionRecord record,
        CancellationToken cancellationToken)
    {
        DiagnosticResponseData[] responses = await context.DiagnosticResponses
            .AsNoTracking()
            .Where(item => item.SessionId == record.Id)
            .OrderBy(item => item.QuestionId)
            .Select(item => new DiagnosticResponseData(
                item.QuestionId,
                item.SelectedOptionId,
                item.SavedAtUtc))
            .ToArrayAsync(cancellationToken);
        DiagnosticPlan plan = JsonSerializer.Deserialize<DiagnosticPlan>(
            record.FrozenPlanJson,
            SerializerOptions)
            ?? throw new InvalidDataException("Le plan figé du diagnostic est illisible.");
        DiagnosticSectionStatus[] statuses = JsonSerializer.Deserialize<DiagnosticSectionStatus[]>(
            record.SectionStatusesJson,
            SerializerOptions)
            ?? throw new InvalidDataException("L'état des sections du diagnostic est illisible.");
        if (statuses.Length != plan.Sections.Count)
        {
            throw new InvalidDataException("L'état des sections ne correspond pas au plan figé.");
        }

        var timeline = new DiagnosticTimeline(
            record.Status,
            record.CurrentSectionIndex,
            Array.AsReadOnly(statuses),
            record.SectionStartedAtUtc,
            record.SectionDeadlineUtc);
        return new DiagnosticSessionData(
            record.Id,
            record.ProfileId,
            record.BankId,
            record.BankVersion,
            record.BankRevision,
            plan,
            timeline,
            record.SectionDurationSeconds,
            record.StartedAtUtc,
            record.UpdatedAtUtc,
            record.EndedAtUtc,
            Array.AsReadOnly(responses));
    }

    private static string SerializePlan(DiagnosticPlan plan)
    {
        string json = JsonSerializer.Serialize(plan, SerializerOptions);
        if (json.Length > MaximumPlanJsonLength)
        {
            throw new InvalidDataException("Le plan figé du diagnostic dépasse la taille autorisée.");
        }

        return json;
    }

    private static string SerializeStatuses(IReadOnlyList<DiagnosticSectionStatus> statuses) =>
        JsonSerializer.Serialize(statuses, SerializerOptions);

    private static void ValidateKeys(Guid profileId, Guid sessionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
    }

    private static void ValidateResponse(DiagnosticResponseData response)
    {
        if (string.IsNullOrWhiteSpace(response.QuestionId) || response.QuestionId.Length > 100)
        {
            throw new ArgumentException("L'identifiant de question est invalide.", nameof(response));
        }

        if (string.IsNullOrWhiteSpace(response.SelectedOptionId) || response.SelectedOptionId.Length > 32)
        {
            throw new ArgumentException("L'identifiant de réponse est invalide.", nameof(response));
        }
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
