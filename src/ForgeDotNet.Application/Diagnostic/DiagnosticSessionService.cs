using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.Diagnostic;

public sealed class DiagnosticSessionService(
    IDiagnosticBankSource bankSource,
    IDiagnosticSessionRepository repository,
    ILocalProfileRepository profileRepository,
    DiagnosticSessionCoordinator coordinator,
    DiagnosticSessionOptions options,
    TimeProvider timeProvider)
{
    public async ValueTask<DiagnosticOverviewView> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        DiagnosticBank bank = await bankSource.GetAsync(cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DiagnosticSessionData? active = await repository.GetActiveAsync(profile.LocalId, cancellationToken);
        if (active is not null)
        {
            active = await RefreshAndPersistAsync(active, cancellationToken);
        }

        DiagnosticSessionData? latest = active
            ?? await repository.GetLatestAsync(profile.LocalId, cancellationToken);
        IReadOnlyList<DiagnosticDomainCoverageView> coverage = DiagnosticDomains.All
            .Select(domain => new DiagnosticDomainCoverageView(
                DiagnosticDomains.GetId(domain),
                DiagnosticDomains.GetDisplayName(domain),
                bank.Questions.Count(question => question.Domain == domain)))
            .ToArray();
        return new DiagnosticOverviewView(
            bank.Title,
            bank.Version,
            bank.Questions.Count,
            coverage,
            active?.Id,
            latest is null ? null : ToSummary(latest));
    }

    public async ValueTask<DiagnosticSessionView> StartAsync(
        DiagnosticMode mode,
        CancellationToken cancellationToken = default)
    {
        DiagnosticBank bank = await bankSource.GetAsync(cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        TimeSpan duration = options.GetSectionDuration(mode);
        Guid sessionId = Guid.NewGuid();
        int seed = BitConverter.ToInt32(sessionId.ToByteArray(), 0);
        DiagnosticPlan plan = DiagnosticSampler.CreatePlan(bank, mode, seed);
        DiagnosticTimeline timeline = DiagnosticTimelineRules.CreateStarted(plan.Sections.Count, now, duration);
        var proposed = new DiagnosticSessionData(
            sessionId,
            profile.LocalId,
            bank.Id,
            bank.Version,
            bank.Revision,
            plan,
            timeline,
            checked((int)duration.TotalSeconds),
            now,
            now,
            null,
            Array.Empty<DiagnosticResponseData>());

        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DiagnosticSessionData session = await repository.CreateOrGetActiveAsync(proposed, cancellationToken);
        session = await RefreshAndPersistAsync(session, cancellationToken);
        return ToView(session, timeProvider.GetUtcNow());
    }

    public async ValueTask<DiagnosticSessionView> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DiagnosticSessionData session = await RequireSessionAsync(profile.LocalId, sessionId, cancellationToken);
        session = await RefreshAndPersistAsync(session, cancellationToken);
        return ToView(session, timeProvider.GetUtcNow());
    }

    public async ValueTask<DiagnosticSessionView> SaveResponseAsync(
        Guid sessionId,
        string questionId,
        string optionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionId);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DiagnosticSessionData session = await RequireSessionAsync(profile.LocalId, sessionId, cancellationToken);
        session = await RefreshAndPersistAsync(session, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        (int sectionIndex, DiagnosticQuestion question) = FindQuestion(session.Plan, questionId);
        if (!DiagnosticTimelineRules.CanAnswer(session.Timeline, sectionIndex, now))
        {
            throw new InvalidOperationException("Le temps de cette section est écoulé ou la section n'est pas active.");
        }

        if (!question.Options.Any(option => string.Equals(option.Id, optionId, StringComparison.Ordinal)))
        {
            throw new ArgumentException("La réponse ne fait pas partie de la question.", nameof(optionId));
        }

        var response = new DiagnosticResponseData(questionId, optionId, now);
        await repository.UpsertResponseAsync(profile.LocalId, sessionId, response, cancellationToken);
        DiagnosticResponseData[] responses = session.Responses
            .Where(item => !string.Equals(item.QuestionId, questionId, StringComparison.Ordinal))
            .Append(response)
            .OrderBy(item => item.QuestionId, StringComparer.Ordinal)
            .ToArray();
        session = session with { Responses = Array.AsReadOnly(responses), UpdatedAtUtc = now };
        return ToView(session, now);
    }

    public ValueTask<DiagnosticSessionView> StartCurrentSectionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) => MutateTimelineAsync(
            sessionId,
            (session, now) => session.Timeline.CurrentSectionIndex < session.Timeline.SectionStatuses.Count
                && session.Timeline.SectionStatuses[session.Timeline.CurrentSectionIndex] == DiagnosticSectionStatus.Active
                    ? session.Timeline
                    : DiagnosticTimelineRules.StartCurrent(
                        session.Timeline,
                        now,
                        TimeSpan.FromSeconds(session.SectionDurationSeconds)),
            endSession: false,
            cancellationToken);

    public ValueTask<DiagnosticSessionView> CompleteSectionAsync(
        Guid sessionId,
        int sectionIndex,
        CancellationToken cancellationToken = default) => MutateTimelineAsync(
            sessionId,
            (session, now) => DiagnosticTimelineRules.CompleteCurrent(session.Timeline, sectionIndex, now),
            endSession: false,
            cancellationToken);

    public ValueTask<DiagnosticSessionView> FinishAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) => MutateTimelineAsync(
            sessionId,
            (session, now) => DiagnosticTimelineRules.Finish(session.Timeline, now),
            endSession: true,
            cancellationToken);

    public ValueTask<DiagnosticSessionView> AbandonAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) => MutateTimelineAsync(
            sessionId,
            (session, _) => DiagnosticTimelineRules.Abandon(session.Timeline),
            endSession: true,
            cancellationToken);

    private async ValueTask<DiagnosticSessionView> MutateTimelineAsync(
        Guid sessionId,
        Func<DiagnosticSessionData, DateTimeOffset, DiagnosticTimeline> mutation,
        bool endSession,
        CancellationToken cancellationToken)
    {
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DiagnosticSessionData session = await RequireSessionAsync(profile.LocalId, sessionId, cancellationToken);
        session = await RefreshAndPersistAsync(session, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        DiagnosticTimeline timeline = mutation(session, now);
        DateTimeOffset? endedAt = endSession && timeline.SessionStatus != DiagnosticSessionStatus.Active
            ? session.EndedAtUtc ?? now
            : session.EndedAtUtc;
        if (!Equals(timeline, session.Timeline) || endedAt != session.EndedAtUtc)
        {
            await repository.SaveTimelineAsync(
                profile.LocalId,
                sessionId,
                timeline,
                now,
                endedAt,
                cancellationToken);
            session = session with { Timeline = timeline, UpdatedAtUtc = now, EndedAtUtc = endedAt };
        }

        return ToView(session, now);
    }

    private async ValueTask<DiagnosticSessionData> RefreshAndPersistAsync(
        DiagnosticSessionData session,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DiagnosticTimeline refreshed = DiagnosticTimelineRules.Refresh(session.Timeline, now);
        if (Equals(refreshed, session.Timeline))
        {
            return session;
        }

        await repository.SaveTimelineAsync(
            session.ProfileId,
            session.Id,
            refreshed,
            now,
            session.EndedAtUtc,
            cancellationToken);
        return session with { Timeline = refreshed, UpdatedAtUtc = now };
    }

    private async ValueTask<DiagnosticSessionData> RequireSessionAsync(
        Guid profileId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        await repository.GetAsync(profileId, sessionId, cancellationToken)
        ?? throw new KeyNotFoundException("La session de diagnostic demandée n'existe pas.");

    private static (int SectionIndex, DiagnosticQuestion Question) FindQuestion(
        DiagnosticPlan plan,
        string questionId)
    {
        foreach (DiagnosticPlanSection section in plan.Sections)
        {
            DiagnosticQuestion? question = section.Questions.SingleOrDefault(item =>
                string.Equals(item.Id, questionId, StringComparison.Ordinal));
            if (question is not null)
            {
                return (section.Index, question);
            }
        }

        throw new ArgumentException("La question ne fait pas partie de la session figée.", nameof(questionId));
    }

    private static DiagnosticSessionSummaryView ToSummary(DiagnosticSessionData session)
    {
        int total = session.Plan.QuestionCount;
        int answered = session.Responses.Count;
        return new DiagnosticSessionSummaryView(
            session.Id,
            session.Plan.Mode,
            session.Timeline.SessionStatus,
            answered,
            total,
            answered == total,
            session.StartedAtUtc,
            session.EndedAtUtc);
    }

    private static DiagnosticSessionView ToView(DiagnosticSessionData session, DateTimeOffset now)
    {
        Dictionary<string, DiagnosticResponseData> responses = session.Responses
            .ToDictionary(item => item.QuestionId, StringComparer.Ordinal);
        DiagnosticSectionSummaryView[] sections = session.Plan.Sections
            .Select(section => new DiagnosticSectionSummaryView(
                section.Index,
                section.Title,
                session.Timeline.SectionStatuses[section.Index],
                section.Questions.Count(question => responses.ContainsKey(question.Id)),
                section.Questions.Count))
            .ToArray();
        DiagnosticSectionView? current = session.Timeline.CurrentSectionIndex < session.Plan.Sections.Count
            ? ToSectionView(
                session.Plan.Sections[session.Timeline.CurrentSectionIndex],
                session.Timeline.SectionStatuses[session.Timeline.CurrentSectionIndex],
                responses)
            : null;
        int answered = session.Responses.Count;
        return new DiagnosticSessionView(
            session.Id,
            session.BankId,
            session.BankVersion,
            session.BankRevision,
            session.Plan.Mode,
            session.Timeline.SessionStatus,
            Array.AsReadOnly(sections),
            current,
            session.Timeline.CurrentSectionIndex,
            answered,
            session.Plan.QuestionCount,
            answered == session.Plan.QuestionCount,
            session.StartedAtUtc,
            session.Timeline.SectionDeadlineUtc,
            (int)Math.Ceiling(DiagnosticTimelineRules.GetRemaining(session.Timeline, now).TotalSeconds),
            session.EndedAtUtc);
    }

    private static DiagnosticSectionView ToSectionView(
        DiagnosticPlanSection section,
        DiagnosticSectionStatus status,
        Dictionary<string, DiagnosticResponseData> responses)
    {
        DiagnosticQuestionView[] questions = section.Questions
            .Select(question => new DiagnosticQuestionView(
                question.Id,
                DiagnosticDomains.GetId(question.Domain),
                DiagnosticDomains.GetDisplayName(question.Domain),
                question.Difficulty,
                question.Prompt,
                question.Options,
                responses.TryGetValue(question.Id, out DiagnosticResponseData? response)
                    ? response.SelectedOptionId
                    : null))
            .ToArray();
        return new DiagnosticSectionView(
            section.Index,
            section.Title,
            status,
            Array.AsReadOnly(questions));
    }
}
