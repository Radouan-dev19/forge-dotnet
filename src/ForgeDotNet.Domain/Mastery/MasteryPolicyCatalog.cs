namespace ForgeDotNet.Domain.Mastery;

public static class MasteryPolicyCatalog
{
    public const string ConsoleProject = "project.console";
    public const string NinetyMinuteExam = "exam.90-minutes";
    public const string ApiFunctional = "api.functional";
    public const string EfCore = "ef-core";
    public const string ValidationAndErrors = "validation-errors";
    public const string UnitTests = "tests.unit";
    public const string IntegrationTests = "tests.integration";
    public const string CleanGit = "git.clean";
    public const string TenMinutePresentation = "presentation.10-minutes";
    public const string Docker = "docker";
    public const string ContinuousIntegration = "ci";
    public const string AuthenticationAuthorization = "authn-authz";
    public const string Logs = "logs";
    public const string Deployment = "deployment";
    public const string SimulatedIncident = "incident.simulated";
    public const string MockInterview = "interview.mock";
    public const string Performance = "performance";
    public const string Security = "security";
    public const string PragmaticArchitecture = "architecture.pragmatic";
    public const string AutonomousFeature = "feature.autonomous";
    public const string CodeReview = "code-review";
    public const string English = "english";
    public const string FinalDefense = "project.final-defense";

    public static MasteryPolicy Version1 { get; } = CreateVersion1();

    private static MasteryPolicy CreateVersion1() => new(
        "forge-mastery",
        1,
        "mastery-v1-20260729",
        Array.AsReadOnly(
        [
            new MasteryComponentPolicy(MasteryComponent.AutonomousPractice, 0.45m),
            new MasteryComponentPolicy(MasteryComponent.UnassistedExam, 0.25m),
            new MasteryComponentPolicy(MasteryComponent.SpacedRetention, 0.15m),
            new MasteryComponentPolicy(MasteryComponent.Explanation, 0.10m),
            new MasteryComponentPolicy(MasteryComponent.Quiz, 0.05m),
        ]),
        ModuleThreshold: 80m,
        CriticalModuleThreshold: 85m,
        Array.AsReadOnly(
        [
            MasteryDomain.CSharp,
            MasteryDomain.Debugging,
            MasteryDomain.Sql,
            MasteryDomain.Api,
            MasteryDomain.Tests,
        ]),
        MinimumDistinctItems: 3,
        RecentProofDays: 30,
        MaximumEvidenceAgeDays: 90,
        Array.AsReadOnly(
        [
            GateA(),
            GateB(),
            GateC(),
            GateD(),
        ]));

    private static MasteryGatePolicy GateA() => new(
        MasteryGate.A,
        "A — Junior fiable",
        Array.AsReadOnly(
        [
            Domain(MasteryDomain.CSharp, 85m, "C# ≥ 85"),
            Domain(MasteryDomain.Debugging, 80m, "Débogage ≥ 80"),
            Domain(MasteryDomain.Sql, 75m, "SQL ≥ 75"),
            new(MasteryGateRequirementKind.UnassistedExerciseCount, "10 exercices vérifiés sans aide", MinimumCount: 10),
            Achievement(ConsoleProject, "Mini-projet console vérifié"),
            Achievement(NinetyMinuteExam, "Examen sans aide de 90 minutes", 90),
        ]));

    private static MasteryGatePolicy GateB() => new(
        MasteryGate.B,
        "B — Backend .NET",
        WithPrevious(
            MasteryGate.A,
            "Porte A ouverte",
            Achievement(ApiFunctional, "API fonctionnelle"),
            Achievement(EfCore, "EF Core"),
            Achievement(ValidationAndErrors, "Validation et erreurs"),
            Achievement(UnitTests, "Tests unitaires"),
            Achievement(IntegrationTests, "Tests d’intégration"),
            Achievement(CleanGit, "Historique Git propre"),
            Achievement(TenMinutePresentation, "Présentation de 10 minutes", 10)));

    private static MasteryGatePolicy GateC() => new(
        MasteryGate.C,
        "C — Équipe moderne",
        WithPrevious(
            MasteryGate.B,
            "Porte B ouverte",
            Achievement(Docker, "Docker"),
            Achievement(ContinuousIntegration, "Intégration continue"),
            Achievement(AuthenticationAuthorization, "Authentification et autorisation"),
            Achievement(Logs, "Logs exploitables"),
            Achievement(Deployment, "Déploiement"),
            Achievement(SimulatedIncident, "Incident simulé"),
            Achievement(MockInterview, "Entretien blanc")));

    private static MasteryGatePolicy GateD() => new(
        MasteryGate.D,
        "D — Intermédiaire en construction",
        WithPrevious(
            MasteryGate.C,
            "Porte C ouverte",
            Achievement(Performance, "Performance"),
            Achievement(Security, "Sécurité"),
            Achievement(PragmaticArchitecture, "Architecture pragmatique"),
            Achievement(AutonomousFeature, "Fonctionnalité autonome"),
            Achievement(CodeReview, "Revue de code"),
            Achievement(English, "Anglais professionnel"),
            Achievement(FinalDefense, "Défense du projet final")));

    private static MasteryGateRequirement Domain(MasteryDomain domain, decimal score, string label) =>
        new(MasteryGateRequirementKind.DomainScore, label, Domain: domain, MinimumScore: score);

    private static MasteryGateRequirement Achievement(string key, string label, int duration = 0) =>
        new(
            MasteryGateRequirementKind.Achievement,
            label,
            AchievementKey: key,
            MinimumDurationMinutes: duration);

    private static System.Collections.ObjectModel.ReadOnlyCollection<MasteryGateRequirement> WithPrevious(
        MasteryGate previous,
        string previousLabel,
        params MasteryGateRequirement[] requirements)
    {
        var all = new MasteryGateRequirement[requirements.Length + 1];
        all[0] = new(MasteryGateRequirementKind.PreviousGate, previousLabel, PreviousGate: previous);
        Array.Copy(requirements, 0, all, 1, requirements.Length);
        return Array.AsReadOnly(all);
    }
}
