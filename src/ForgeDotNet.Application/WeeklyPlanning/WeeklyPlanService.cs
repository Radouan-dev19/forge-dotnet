using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.Application.WeeklyPlanning;

public sealed class WeeklyPlanService(
    IWeeklyPlanCurriculumSource curriculumSource,
    IWeeklyPlanRepository planRepository,
    IDiagnosticEvaluationRepository evaluationRepository,
    ILocalProfileRepository profileRepository,
    WeeklyPlanCoordinator coordinator,
    TimeProvider timeProvider)
{
    public async ValueTask<WeeklyPlanView> GetOrCreateAsync(
        Guid diagnosticSessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(diagnosticSessionId, Guid.Empty);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        WeeklyPlanData? existing = await planRepository.GetLatestAsync(
            profile.LocalId,
            diagnosticSessionId,
            cancellationToken);
        if (existing is not null)
        {
            return ToView(existing, profile.WeeklyAvailableHours);
        }

        DiagnosticEvaluationData evaluation = await evaluationRepository.GetAsync(
            profile.LocalId,
            diagnosticSessionId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Le diagnostic doit être évalué avant de générer un plan personnalisé.");
        WeeklyPlanCurriculumSnapshot curriculum = await curriculumSource.GetAsync(cancellationToken);
        WeeklyPlanSnapshot snapshot = WeeklyPlanRules.Create(
            diagnosticSessionId,
            evaluation.Report,
            curriculum,
            profile.WeeklyAvailableHours);
        DateTimeOffset now = timeProvider.GetUtcNow();
        var initial = new WeeklyPlanData(
            Guid.NewGuid(),
            profile.LocalId,
            diagnosticSessionId,
            Version: 1,
            WeeklyPlanStatus.Draft,
            snapshot,
            now,
            AcceptedAtUtc: null);
        return ToView(
            await planRepository.CreateInitialOrGetAsync(initial, cancellationToken),
            profile.WeeklyAvailableHours);
    }

    public async ValueTask<WeeklyPlanView> AdjustHoursAsync(
        Guid diagnosticSessionId,
        int expectedVersion,
        int requestedWeeklyHours,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(diagnosticSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        WeeklyPlanData current = await planRepository.GetLatestAsync(
            profile.LocalId,
            diagnosticSessionId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Aucun plan n'existe pour ce diagnostic.");
        if (current.Version != expectedVersion)
        {
            throw new InvalidOperationException("Le plan a changé ; rechargez la version courante avant de l'ajuster.");
        }

        if (current.Status == WeeklyPlanStatus.Accepted)
        {
            throw new InvalidOperationException("Un plan accepté ne peut plus être modifié.");
        }

        WeeklyPlanSnapshot adjusted = WeeklyPlanRules.Reallocate(
            current.Snapshot,
            profile.WeeklyAvailableHours,
            requestedWeeklyHours);
        var next = new WeeklyPlanData(
            Guid.NewGuid(),
            profile.LocalId,
            diagnosticSessionId,
            current.Version + 1,
            WeeklyPlanStatus.Draft,
            adjusted,
            timeProvider.GetUtcNow(),
            AcceptedAtUtc: null);
        return ToView(
            await planRepository.CreateNextVersionAsync(
                next,
                expectedVersion,
                cancellationToken),
            profile.WeeklyAvailableHours);
    }

    public async ValueTask<WeeklyPlanView> AcceptAsync(
        Guid diagnosticSessionId,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(diagnosticSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        WeeklyPlanData accepted = await planRepository.AcceptAsync(
            profile.LocalId,
            diagnosticSessionId,
            expectedVersion,
            timeProvider.GetUtcNow(),
            cancellationToken);
        return ToView(accepted, profile.WeeklyAvailableHours);
    }

    private static WeeklyPlanView ToView(WeeklyPlanData data, int currentProfileAvailableHours)
    {
        WeeklyPlanSnapshot snapshot = data.Snapshot;
        WeeklyPlanRecommendationView[] recommendations = snapshot.Recommendations
            .OrderBy(item => item.Priority)
            .Select(item => new WeeklyPlanRecommendationView(
                DiagnosticDomains.GetId(item.Domain),
                item.DisplayName,
                item.IsCritical,
                item.Kind,
                GetRecommendationLabel(item.Kind),
                item.Priority,
                item.DiagnosticScore,
                item.Rationale))
            .ToArray();
        WeeklyPlanWeekView[] weeks = snapshot.Weeks
            .OrderBy(item => item.Number)
            .Select(item => new WeeklyPlanWeekView(
                item.CurriculumWeekId,
                item.Number,
                item.Title,
                item.Prerequisites,
                item.PlannedHours,
                item.CoreLearningHours,
                item.RemediationHours,
                item.ConsolidationHours,
                item.KnowledgeCheckHours,
                item.KnowledgeCheckRequired,
                item.Focuses.Select(focus => new WeeklyPlanWeekFocusView(
                    DiagnosticDomains.GetId(focus.Domain),
                    focus.DisplayName,
                    focus.Depth,
                    GetDepthLabel(focus.Depth))).ToArray(),
                item.Explanation))
            .ToArray();
        return new WeeklyPlanView(
            data.Id,
            data.DiagnosticSessionId,
            data.Version,
            data.Status,
            data.Status == WeeklyPlanStatus.Draft ? "Proposition à valider" : "Plan accepté",
            snapshot.Curriculum.Id,
            snapshot.Curriculum.Version,
            snapshot.Curriculum.Revision,
            snapshot.ProfileAvailableHours,
            currentProfileAvailableHours,
            snapshot.TargetWeeklyHours,
            snapshot.IsProvisional,
            data.Status == WeeklyPlanStatus.Draft,
            snapshot.Warnings,
            Array.AsReadOnly(recommendations),
            Array.AsReadOnly(weeks),
            data.CreatedAtUtc,
            data.AcceptedAtUtc);
    }

    private static string GetRecommendationLabel(WeeklyPlanRecommendationKind kind) => kind switch
    {
        WeeklyPlanRecommendationKind.CriticalRemediation => "Remédiation critique prioritaire",
        WeeklyPlanRecommendationKind.EvidenceToCollect => "Preuves à compléter",
        WeeklyPlanRecommendationKind.Remediation => "Fondamentaux à reprendre",
        WeeklyPlanRecommendationKind.Reinforce => "Repères à consolider",
        WeeklyPlanRecommendationKind.CondenseAndVerify => "Étude condensée, contrôle conservé",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GetDepthLabel(WeeklyPlanDepth depth) => depth switch
    {
        WeeklyPlanDepth.FullWithRemediation => "Parcours complet avec remédiation",
        WeeklyPlanDepth.EvidenceFirst => "Bases puis collecte de preuves",
        WeeklyPlanDepth.Full => "Parcours complet",
        WeeklyPlanDepth.CondensedWithVerification => "Parcours condensé avec contrôle",
        _ => throw new ArgumentOutOfRangeException(nameof(depth)),
    };
}
