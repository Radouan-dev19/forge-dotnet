namespace ForgeDotNet.Domain.Projects;

/// <summary>Jalon d'un projet, tel que le brief l'annonce à l'apprenant.</summary>
public sealed record ProjectMilestone(
    string Id,
    string Title,
    string Evidence,
    IReadOnlyList<string> AcceptanceCriteria);

/// <summary>Critère de la grille : observé par un humain, jamais par une suite.</summary>
public sealed record ProjectRubricCriterion(string Criterion, decimal Weight, string ObservableEvidence);

/// <summary>
/// Suite d'acceptation attachée à un jalon. Son dossier porte le même contrat qu'un exercice —
/// <c>tests/runner.json</c>, <c>tests/visible/cases.json</c>, <c>tests/hidden/cases.json</c> — ce qui
/// lui vaut d'être exécutée par le même bac à sable, sans quota ni surface nouvelle.
/// </summary>
public sealed record ProjectAcceptanceSuite(string MilestoneId, string SuitePath)
{
    /// <summary>
    /// Identifiant d'exécution de la suite, de la forme <c>&lt;projet&gt;.&lt;jalon&gt;</c>.
    /// </summary>
    public static string RunIdentifier(string projectId, string milestoneId) => $"{projectId}.{milestoneId}";
}

/// <summary>Fichier remis à l'apprenant au démarrage du projet.</summary>
public sealed record ProjectStarterFile(string FileName, string Content);

/// <summary>
/// Projet publié. Un projet sans suite d'acceptation reste un livrable guidé : il s'affiche, se
/// travaille, mais ne peut produire aucune preuve automatique.
/// </summary>
public sealed record Project(
    string Id,
    int Version,
    string Title,
    int Difficulty,
    IReadOnlyList<int> Weeks,
    IReadOnlyList<string> Skills,
    int EstimatedHours,
    string Brief,
    IReadOnlyList<ProjectStarterFile> StarterFiles,
    int MaximumSourceFiles,
    IReadOnlyList<ProjectMilestone> Milestones,
    IReadOnlyList<ProjectRubricCriterion> Rubric,
    IReadOnlyList<ProjectAcceptanceSuite> AcceptanceSuites,
    IReadOnlyList<string> CommonMistakes,
    string? AchievementKey,
    string Revision)
{
    /// <summary>
    /// Vrai lorsque le projet peut être vérifié automatiquement, donc produire une preuve.
    /// </summary>
    public bool IsVerifiable => AcceptanceSuites.Count > 0 && StarterFiles.Count > 0;

    /// <summary>
    /// Vrai lorsque le projet est rattaché à une exigence de porte qu'il peut satisfaire.
    /// </summary>
    /// <remarks>
    /// La clé est déclarée par le contenu et non déduite du code : c'est le brief qui sait à quelle
    /// exigence il répond, et une clé absente signifie simplement « ce projet ne prouve rien ».
    /// </remarks>
    public bool ProducesAchievement => IsVerifiable && !string.IsNullOrWhiteSpace(AchievementKey);
}
