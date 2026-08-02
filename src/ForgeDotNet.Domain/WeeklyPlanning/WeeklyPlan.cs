using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Domain.WeeklyPlanning;

public enum WeeklyPlanRecommendationKind
{
    CriticalRemediation,
    EvidenceToCollect,
    Remediation,
    Reinforce,
    CondenseAndVerify,
}

public enum WeeklyPlanDepth
{
    FullWithRemediation,
    EvidenceFirst,
    Full,
    CondensedWithVerification,
}

public enum WeeklyPlanStatus
{
    Draft,
    Accepted,
}

public sealed record WeeklyPlanCurriculumWeek(
    string Id,
    int Number,
    string Title,
    IReadOnlyList<DiagnosticDomain> Domains,
    IReadOnlyList<string> Prerequisites);

public sealed record WeeklyPlanCurriculumSnapshot(
    string Id,
    int Version,
    string Revision,
    IReadOnlyList<WeeklyPlanCurriculumWeek> Weeks);

public sealed record WeeklyPlanRecommendation(
    DiagnosticDomain Domain,
    string DisplayName,
    bool IsCritical,
    WeeklyPlanRecommendationKind Kind,
    int Priority,
    decimal DiagnosticScore,
    string Rationale);

public sealed record WeeklyPlanWeekFocus(
    DiagnosticDomain Domain,
    string DisplayName,
    WeeklyPlanRecommendationKind Recommendation,
    WeeklyPlanDepth Depth);

public sealed record WeeklyPlanWeek(
    string CurriculumWeekId,
    int Number,
    string Title,
    IReadOnlyList<string> Prerequisites,
    int PlannedHours,
    decimal CoreLearningHours,
    decimal RemediationHours,
    decimal ConsolidationHours,
    decimal KnowledgeCheckHours,
    bool KnowledgeCheckRequired,
    IReadOnlyList<WeeklyPlanWeekFocus> Focuses,
    string Explanation);

public sealed record WeeklyPlanSnapshot(
    Guid DiagnosticSessionId,
    string EvaluationRubricId,
    int EvaluationRubricVersion,
    string EvaluationRubricRevision,
    WeeklyPlanCurriculumSnapshot Curriculum,
    int ProfileAvailableHours,
    int TargetWeeklyHours,
    bool IsProvisional,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<WeeklyPlanRecommendation> Recommendations,
    IReadOnlyList<WeeklyPlanWeek> Weeks);

public static class WeeklyPlanRules
{
    public const int PreferredMinimumWeeklyHours = 10;
    public const int MaximumWeeklyHours = 15;

    public static WeeklyPlanSnapshot Create(
        Guid diagnosticSessionId,
        DiagnosticEvaluationReport evaluation,
        WeeklyPlanCurriculumSnapshot curriculum,
        int profileAvailableHours,
        int? requestedWeeklyHours = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(diagnosticSessionId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(evaluation);
        ValidateCurriculum(curriculum);
        ValidateEvaluation(evaluation);
        int targetWeeklyHours = ResolveTargetHours(profileAvailableHours, requestedWeeklyHours);
        WeeklyPlanRecommendation[] recommendations = CreateRecommendations(evaluation);

        return CreateSnapshot(
            diagnosticSessionId,
            evaluation.Rubric.Id,
            evaluation.Rubric.Version,
            evaluation.Rubric.Revision,
            curriculum,
            profileAvailableHours,
            targetWeeklyHours,
            evaluation.IsProvisional,
            recommendations);
    }

    public static WeeklyPlanSnapshot Reallocate(
        WeeklyPlanSnapshot current,
        int profileAvailableHours,
        int requestedWeeklyHours)
    {
        ArgumentNullException.ThrowIfNull(current);
        ValidateCurriculum(current.Curriculum);
        ValidateRecommendations(current.Recommendations);
        int targetWeeklyHours = ResolveTargetHours(profileAvailableHours, requestedWeeklyHours);
        return CreateSnapshot(
            current.DiagnosticSessionId,
            current.EvaluationRubricId,
            current.EvaluationRubricVersion,
            current.EvaluationRubricRevision,
            current.Curriculum,
            profileAvailableHours,
            targetWeeklyHours,
            current.IsProvisional,
            current.Recommendations);
    }

    public static void ValidateCurriculum(WeeklyPlanCurriculumSnapshot curriculum)
    {
        ArgumentNullException.ThrowIfNull(curriculum);
        if (string.IsNullOrWhiteSpace(curriculum.Id)
            || curriculum.Version < 1
            || curriculum.Revision.Length != 64
            || curriculum.Weeks.Count != 24)
        {
            throw new InvalidDataException("L'identité du curriculum de planification est invalide.");
        }

        WeeklyPlanCurriculumWeek[] weeks = curriculum.Weeks.OrderBy(week => week.Number).ToArray();
        if (weeks.Select(week => week.Id).Distinct(StringComparer.Ordinal).Count() != weeks.Length
            || weeks.Select(week => week.Number).Distinct().Count() != weeks.Length
            || weeks.Where((week, index) => week.Number != index + 1).Any())
        {
            throw new InvalidDataException("Les semaines du curriculum doivent former une séquence unique commençant à 1.");
        }

        var byId = weeks.ToDictionary(week => week.Id, StringComparer.Ordinal);
        foreach (WeeklyPlanCurriculumWeek week in weeks)
        {
            if (string.IsNullOrWhiteSpace(week.Id)
                || string.IsNullOrWhiteSpace(week.Title)
                || week.Domains.Count == 0
                || week.Domains.Distinct().Count() != week.Domains.Count
                || week.Domains.Any(domain => !DiagnosticDomains.All.Contains(domain))
                || week.Prerequisites.Distinct(StringComparer.Ordinal).Count() != week.Prerequisites.Count)
            {
                throw new InvalidDataException($"La semaine {week.Number} du curriculum est invalide.");
            }

            foreach (string prerequisite in week.Prerequisites)
            {
                if (!byId.TryGetValue(prerequisite, out WeeklyPlanCurriculumWeek? prerequisiteWeek)
                    || prerequisiteWeek.Number >= week.Number)
                {
                    throw new InvalidDataException(
                        $"Le prérequis '{prerequisite}' de la semaine {week.Number} est absent, cyclique ou placé après elle.");
                }
            }
        }

        DiagnosticDomain[] coveredDomains = weeks
            .SelectMany(week => week.Domains)
            .Distinct()
            .ToArray();
        if (coveredDomains.Length != DiagnosticDomains.All.Count)
        {
            throw new InvalidDataException("Le curriculum ne couvre pas tous les domaines du diagnostic.");
        }
    }

    public static void ValidateSnapshot(WeeklyPlanSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfEqual(snapshot.DiagnosticSessionId, Guid.Empty);
        ValidateCurriculum(snapshot.Curriculum);
        ValidateRecommendations(snapshot.Recommendations);
        if (string.IsNullOrWhiteSpace(snapshot.EvaluationRubricId)
            || snapshot.EvaluationRubricVersion < 1
            || snapshot.EvaluationRubricRevision.Length != 64)
        {
            throw new InvalidDataException("La référence à l'évaluation du plan est invalide.");
        }

        _ = ResolveTargetHours(snapshot.ProfileAvailableHours, snapshot.TargetWeeklyHours);
        if (snapshot.Weeks.Count != snapshot.Curriculum.Weeks.Count || snapshot.Warnings.Count == 0)
        {
            throw new InvalidDataException("Le plan hebdomadaire est incomplet.");
        }

        var curriculumById = snapshot.Curriculum.Weeks.ToDictionary(week => week.Id, StringComparer.Ordinal);
        var recommendationsByDomain = snapshot.Recommendations.ToDictionary(item => item.Domain);
        foreach (WeeklyPlanWeek week in snapshot.Weeks)
        {
            if (!curriculumById.TryGetValue(week.CurriculumWeekId, out WeeklyPlanCurriculumWeek? curriculumWeek)
                || curriculumWeek.Number != week.Number
                || !string.Equals(curriculumWeek.Title, week.Title, StringComparison.Ordinal)
                || !curriculumWeek.Prerequisites.SequenceEqual(week.Prerequisites, StringComparer.Ordinal)
                || week.PlannedHours != snapshot.TargetWeeklyHours
                || !week.KnowledgeCheckRequired
                || week.Focuses.Count == 0
                || week.Focuses.Select(focus => focus.Domain).Distinct().Count() != week.Focuses.Count
                || curriculumWeek.Domains.Except(week.Focuses.Select(focus => focus.Domain)).Any()
                || week.Focuses.Select(focus => focus.Domain).Except(curriculumWeek.Domains).Any()
                || week.Focuses.Any(focus =>
                    !recommendationsByDomain.TryGetValue(focus.Domain, out WeeklyPlanRecommendation? recommendation)
                    || !string.Equals(focus.DisplayName, recommendation.DisplayName, StringComparison.Ordinal)
                    || focus.Recommendation != recommendation.Kind
                    || focus.Depth != GetDepth(recommendation.Kind))
                || week.CoreLearningHours < 0m
                || week.RemediationHours < 0m
                || week.ConsolidationHours < 0m
                || week.KnowledgeCheckHours <= 0m
                || week.CoreLearningHours + week.RemediationHours + week.ConsolidationHours + week.KnowledgeCheckHours != week.PlannedHours)
            {
                throw new InvalidDataException($"La semaine {week.Number} du plan est incohérente.");
            }
        }

        DiagnosticDomain[] scheduledCriticalRemediations = snapshot.Weeks
            .SelectMany(week => week.Focuses)
            .Where(focus => focus.Recommendation == WeeklyPlanRecommendationKind.CriticalRemediation)
            .Select(focus => focus.Domain)
            .Distinct()
            .ToArray();
        DiagnosticDomain[] requiredCriticalRemediations = snapshot.Recommendations
            .Where(item => item.Kind == WeeklyPlanRecommendationKind.CriticalRemediation)
            .Select(item => item.Domain)
            .ToArray();
        if (requiredCriticalRemediations.Except(scheduledCriticalRemediations).Any())
        {
            throw new InvalidDataException("Une lacune critique n'est pas planifiée.");
        }
    }

    private static WeeklyPlanSnapshot CreateSnapshot(
        Guid diagnosticSessionId,
        string rubricId,
        int rubricVersion,
        string rubricRevision,
        WeeklyPlanCurriculumSnapshot curriculum,
        int profileAvailableHours,
        int targetWeeklyHours,
        bool isProvisional,
        IReadOnlyList<WeeklyPlanRecommendation> recommendations)
    {
        IReadOnlyDictionary<DiagnosticDomain, WeeklyPlanRecommendation> byDomain = recommendations
            .ToDictionary(recommendation => recommendation.Domain);
        WeeklyPlanWeek[] weeks = curriculum.Weeks
            .OrderBy(week => week.Number)
            .Select(week => CreateWeek(week, targetWeeklyHours, byDomain))
            .ToArray();
        string[] warnings = CreateWarnings(profileAvailableHours, targetWeeklyHours, isProvisional);

        var snapshot = new WeeklyPlanSnapshot(
            diagnosticSessionId,
            rubricId,
            rubricVersion,
            rubricRevision,
            curriculum,
            profileAvailableHours,
            targetWeeklyHours,
            isProvisional,
            Array.AsReadOnly(warnings),
            Array.AsReadOnly(recommendations.ToArray()),
            Array.AsReadOnly(weeks));
        ValidateSnapshot(snapshot);
        return snapshot;
    }

    private static WeeklyPlanRecommendation[] CreateRecommendations(DiagnosticEvaluationReport evaluation)
    {
        var gaps = evaluation.CriticalGaps.ToDictionary(gap => gap.Domain);
        WeeklyPlanRecommendation[] recommendations = evaluation.Domains
            .Select(domain => CreateRecommendation(domain, gaps.ContainsKey(domain.Domain)))
            .OrderBy(recommendation => GetPriorityGroup(recommendation.Kind))
            .ThenByDescending(recommendation => recommendation.IsCritical)
            .ThenBy(recommendation => recommendation.DiagnosticScore)
            .ThenBy(recommendation => Array.IndexOf(DiagnosticDomains.All.ToArray(), recommendation.Domain))
            .Select((recommendation, index) => recommendation with { Priority = index + 1 })
            .ToArray();
        ValidateRecommendations(recommendations);
        return recommendations;
    }

    private static WeeklyPlanRecommendation CreateRecommendation(
        DiagnosticDomainEvaluation evaluation,
        bool isCriticalGap)
    {
        WeeklyPlanRecommendationKind kind;
        string rationale;
        if (evaluation.AnsweredQuestionCount == 0)
        {
            kind = WeeklyPlanRecommendationKind.EvidenceToCollect;
            rationale = "Aucune observation n'est disponible : revoir les bases puis conserver un contrôle sans aide.";
        }
        else if (isCriticalGap)
        {
            kind = WeeklyPlanRecommendationKind.CriticalRemediation;
            rationale = "Lacune critique détectée : la remédiation reste obligatoire et ne peut pas être compensée par la moyenne.";
        }
        else if (evaluation.Measure.Score < 50m)
        {
            kind = WeeklyPlanRecommendationKind.Remediation;
            rationale = "Le score observé appelle une reprise structurée des fondamentaux avant consolidation.";
        }
        else if (evaluation.Measure.Score < 75m)
        {
            kind = WeeklyPlanRecommendationKind.Reinforce;
            rationale = "Les repères sont présents mais demandent une consolidation et une vérification sans aide.";
        }
        else
        {
            kind = WeeklyPlanRecommendationKind.CondenseAndVerify;
            rationale = "Le diagnostic permet de condenser l'étude, jamais de supprimer le contrôle prévu.";
        }

        return new WeeklyPlanRecommendation(
            evaluation.Domain,
            DiagnosticDomains.GetDisplayName(evaluation.Domain),
            evaluation.IsCritical,
            kind,
            Priority: 0,
            evaluation.Measure.Score,
            rationale);
    }

    private static WeeklyPlanWeek CreateWeek(
        WeeklyPlanCurriculumWeek curriculumWeek,
        int targetWeeklyHours,
        IReadOnlyDictionary<DiagnosticDomain, WeeklyPlanRecommendation> recommendations)
    {
        WeeklyPlanRecommendation[] focuses = curriculumWeek.Domains
            .Select(domain => recommendations[domain])
            .OrderBy(recommendation => recommendation.Priority)
            .ToArray();
        WeeklyPlanRecommendationKind leadingKind = focuses
            .OrderBy(focus => GetPriorityGroup(focus.Kind))
            .First()
            .Kind;
        (decimal coreRatio, decimal remediationRatio, string explanation) = leadingKind switch
        {
            WeeklyPlanRecommendationKind.CriticalRemediation =>
                (0.45m, 0.30m, "Charge renforcée sur une lacune critique, avec contrôle final conservé."),
            WeeklyPlanRecommendationKind.EvidenceToCollect =>
                (0.45m, 0.25m, "Reprise prudente faute de preuves suffisantes, puis contrôle sans aide."),
            WeeklyPlanRecommendationKind.Remediation =>
                (0.50m, 0.20m, "Reprise des fondamentaux avant consolidation et contrôle."),
            WeeklyPlanRecommendationKind.Reinforce =>
                (0.55m, 0.10m, "Consolidation des repères observés avec contrôle sans aide."),
            WeeklyPlanRecommendationKind.CondenseAndVerify =>
                (0.35m, 0m, "Étude condensée ; le temps libéré va à la consolidation, sans supprimer le contrôle."),
            _ => throw new InvalidDataException("La recommandation hebdomadaire est inconnue."),
        };

        decimal knowledgeCheckHours = RoundHours(targetWeeklyHours * 0.15m);
        decimal coreLearningHours = RoundHours(targetWeeklyHours * coreRatio);
        decimal remediationHours = RoundHours(targetWeeklyHours * remediationRatio);
        decimal consolidationHours = targetWeeklyHours
            - coreLearningHours
            - remediationHours
            - knowledgeCheckHours;
        if (consolidationHours < 0m)
        {
            throw new InvalidDataException("La répartition horaire calculée est invalide.");
        }

        WeeklyPlanWeekFocus[] weekFocuses = focuses
            .Select(focus => new WeeklyPlanWeekFocus(
                focus.Domain,
                focus.DisplayName,
                focus.Kind,
                GetDepth(focus.Kind)))
            .ToArray();
        return new WeeklyPlanWeek(
            curriculumWeek.Id,
            curriculumWeek.Number,
            curriculumWeek.Title,
            Array.AsReadOnly(curriculumWeek.Prerequisites.ToArray()),
            targetWeeklyHours,
            coreLearningHours,
            remediationHours,
            consolidationHours,
            knowledgeCheckHours,
            KnowledgeCheckRequired: true,
            Array.AsReadOnly(weekFocuses),
            explanation);
    }

    private static int ResolveTargetHours(int profileAvailableHours, int? requestedWeeklyHours)
    {
        if (profileAvailableHours is < 1 or > 40)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profileAvailableHours),
                "Les disponibilités hebdomadaires doivent être comprises entre 1 et 40 heures.");
        }

        int maximumFeasibleHours = Math.Min(profileAvailableHours, MaximumWeeklyHours);
        int target = requestedWeeklyHours ?? maximumFeasibleHours;
        if (target < 1 || target > maximumFeasibleHours)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedWeeklyHours),
                $"La charge doit être comprise entre 1 et {maximumFeasibleHours} heure(s) pour ce profil.");
        }

        return target;
    }

    private static string[] CreateWarnings(
        int profileAvailableHours,
        int targetWeeklyHours,
        bool isProvisional)
    {
        var warnings = new List<string>();
        if (isProvisional)
        {
            warnings.Add("Plan provisoire : le diagnostic ne fournit pas encore une profondeur de preuve complète.");
        }

        if (profileAvailableHours < PreferredMinimumWeeklyHours)
        {
            warnings.Add(
                $"Disponibilité inférieure à la cible de 10–15 h : la charge est limitée à {targetWeeklyHours} h et le rythme devra être réévalué.");
        }
        else if (profileAvailableHours > MaximumWeeklyHours)
        {
            warnings.Add("La charge est plafonnée à 15 h par semaine malgré une disponibilité supérieure.");
        }

        warnings.Add(
            "Ce plan organise les thèmes du curriculum ; il ne prétend pas rendre disponibles les activités des incréments futurs.");
        return warnings.ToArray();
    }

    private static void ValidateEvaluation(DiagnosticEvaluationReport evaluation)
    {
        if (evaluation.Domains.Count != DiagnosticDomains.All.Count
            || evaluation.Domains.Select(domain => domain.Domain).Distinct().Count() != DiagnosticDomains.All.Count
            || evaluation.Domains.Any(domain => !DiagnosticDomains.All.Contains(domain.Domain)))
        {
            throw new InvalidDataException("La carte de compétences du diagnostic est incomplète.");
        }
    }

    private static void ValidateRecommendations(IReadOnlyList<WeeklyPlanRecommendation> recommendations)
    {
        if (recommendations.Count != DiagnosticDomains.All.Count
            || recommendations.Select(item => item.Domain).Distinct().Count() != DiagnosticDomains.All.Count
            || recommendations.Select(item => item.Priority).Order().Where((priority, index) => priority != index + 1).Any())
        {
            throw new InvalidDataException("Les recommandations du plan sont incomplètes ou non ordonnées.");
        }
    }

    private static int GetPriorityGroup(WeeklyPlanRecommendationKind kind) => kind switch
    {
        WeeklyPlanRecommendationKind.CriticalRemediation => 0,
        WeeklyPlanRecommendationKind.EvidenceToCollect => 1,
        WeeklyPlanRecommendationKind.Remediation => 2,
        WeeklyPlanRecommendationKind.Reinforce => 3,
        WeeklyPlanRecommendationKind.CondenseAndVerify => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static WeeklyPlanDepth GetDepth(WeeklyPlanRecommendationKind kind) => kind switch
    {
        WeeklyPlanRecommendationKind.CriticalRemediation => WeeklyPlanDepth.FullWithRemediation,
        WeeklyPlanRecommendationKind.EvidenceToCollect => WeeklyPlanDepth.EvidenceFirst,
        WeeklyPlanRecommendationKind.Remediation => WeeklyPlanDepth.FullWithRemediation,
        WeeklyPlanRecommendationKind.Reinforce => WeeklyPlanDepth.Full,
        WeeklyPlanRecommendationKind.CondenseAndVerify => WeeklyPlanDepth.CondensedWithVerification,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static decimal RoundHours(decimal value) =>
        value == 0m
            ? 0m
            : Math.Max(0.1m, Math.Round(value, 1, MidpointRounding.AwayFromZero));
}
