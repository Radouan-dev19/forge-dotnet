using System.Text.RegularExpressions;
using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P4 — Faible SQL : fort partout ailleurs, faible sur les requêtes réelles. SqlLab désactivé doit
/// rester honnête ; activé, il isole chaque session, refuse l'inter-base, explique l'échec sans
/// livrer la référence, et la porte concernée reste fermée sans compensation.
/// </summary>
[Trait("Category", "Persona")]
public sealed partial class P4FaibleSqlTests
{
    private const string ScenarioId = "sql-active-customers-001";
    private static readonly string[] StrongExercises =
        ["reference-total-001", "algo-binary-search-001"];

    [Fact]
    public async Task P4_FaibleSql_NEstJamaisCompenseParSesForces()
    {
        var registry = new EvidenceRegistry("p4-faible-sql", "P4 — Faible SQL");
        // Démarrage volontaire SANS SqlLab : le mode indisponible doit être honnête.
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p4", PersonaRunnerMode.Docker, sqlLabEnabled: false);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Quatre");
        await page.FillAsync("#professional-goal", "Compenser une vraie faiblesse SQL par des forces ailleurs.");
        await page.FillAsync("#weekly-hours", "10");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");

        await session.GoAsync(app.BaseUrl, "/sql-lab");
        await page.WaitForSelectorAsync("text=SqlLab reste indisponible");
        await registry.CaptureAsync(page, "SqlLab désactivé honnête",
            "Le message annonce l'indisponibilité sans affirmer aucune validation automatique.");
        long earlySqlObservations = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM SqlLearningAttempts");
        Assert.True(earlySqlObservations == 0, "Aucune preuve SQL ne doit exister en mode indisponible.");

        // Forces non SQL réelles : deux exercices C# validés en conteneur.
        foreach (string exerciseId in StrongExercises)
        {
            await SolveAsync(session, app.BaseUrl, exerciseId);
        }

        await registry.CaptureAsync(page, "Forces non SQL constituées",
            "Deux exercices C# réussis dans le bac à sable : le profil contrasté est en place.");

        // SqlLab activé sur les mêmes données : le service local documenté est démarré.
        await app.RestartAsync(PersonaRunnerMode.Docker, sqlLabEnabled: true);
        await session.GoAsync(app.BaseUrl, "/sql-lab");
        await page.WaitForSelectorAsync("#sql-scenario");
        await page.SelectOptionAsync("#sql-scenario", ScenarioId);
        await page.WaitForSelectorAsync("text=Colonnes attendues");
        await page.ClickAsync("button:has-text('Créer une session jetable')");
        await page.WaitForSelectorAsync("text=Session génération");
        await registry.CaptureAsync(page, "Session jetable provisionnée",
            $"Le scénario {ScenarioId} est choisi et sa base dédiée est provisionnée.");

        // Requête incorrecte : erreur pédagogique, sans fuite du résultat de référence.
        await page.FillAsync("#sql-query", "SELECT CustomerId FROM dbo.Customers ORDER BY CustomerId;");
        await page.ClickAsync("button:has-text('Exécuter')");
        await page.WaitForSelectorAsync("text=Validation serveur");
        string invalidRun = await page.InnerTextAsync("section[aria-labelledby='sql-result-title']");
        Assert.Contains("Résultat non conforme.", invalidRun, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Échec SQL expliqué sans fuite",
            "La validation échoue avec des écarts nommés ; le résultat de référence n'est jamais affiché.");

        // Requête nominale : la solution publiée du scénario passe la validation.
        string nominalQuery = ReadScenarioSolutionQuery();
        await page.FillAsync("#sql-query", nominalQuery);
        await page.ClickAsync("button:has-text('Exécuter')");
        await page.WaitForSelectorAsync("text=Résultat conforme.");
        await registry.CaptureAsync(page, "Requête nominale validée",
            "La requête correcte est déclarée conforme par la comparaison serveur.");

        // Attaque inter-base : refusée par le service et par les permissions.
        await page.FillAsync("#sql-query", "SELECT name FROM master.sys.databases;");
        await page.ClickAsync("button:has-text('Exécuter')");
        // Le panneau de résultat précédent reste affiché pendant l'exécution : attendre son remplacement.
        await Assertions.Expect(page.Locator("section[aria-labelledby='sql-result-title']"))
            .Not.ToContainTextAsync("Résultat conforme.", new LocatorAssertionsToContainTextOptions { Timeout = 30_000 });
        string attack = await page.InnerTextAsync("section[aria-labelledby='sql-result-title']");
        Assert.DoesNotContain("Résultat conforme.", attack, StringComparison.Ordinal);
        Assert.DoesNotContain("forge", attack, StringComparison.OrdinalIgnoreCase);
        await registry.CaptureAsync(page, "Inter-base refusé",
            "La requête vers master est rejetée sans lister les bases du serveur.");

        // Reset : la base est reprovisionnée depuis le même jeu de données.
        await page.ClickAsync("button:has-text('Réinitialiser')");
        await page.WaitForSelectorAsync("text=reprovisionnés depuis le même jeu de données");
        await registry.CaptureAsync(page, "Reset prouvé",
            "Base et login détruits puis reprovisionnés ; la génération de session s'incrémente.");

        // Maîtrise et porte : SQL sous seuil malgré les forces, aucune compensation.
        await session.GoAsync(app.BaseUrl, "/mastery");
        ILocator sqlArticle = page.Locator("article.practice-panel:has(h3:text-matches('^SQL —'))");
        string sqlText = await sqlArticle.InnerTextAsync();
        Assert.Contains("non validée", sqlText, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "SQL sous seuil visible",
            "Le domaine SQL reste non validé et ses blocages sont nommés, à côté des forces C#.");

        await session.GoAsync(app.BaseUrl, "/dashboard");
        string gates = await page.InnerTextAsync("section[aria-labelledby='gates-title']");
        IReadOnlyList<string> gateStates = await page
            .Locator("section[aria-labelledby='gates-title'] article h3")
            .AllInnerTextsAsync();
        Assert.All(gateStates, state => Assert.EndsWith("fermée", state));
        Assert.Contains("SQL", gates, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Porte fermée sans compensation",
            "La porte A liste explicitement l'exigence SQL non satisfaite : aucune moyenne ne la compense.");

        long sqlObservations = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM SqlLearningAttempts");
        registry.Note("État persistant",
            $"SqlLearningAttempts = {sqlObservations} après les exécutions validées ; la progression SQLite du "
            + "profil n'a jamais été atteinte par les sessions SQL, provisionnées dans des bases jetables dédiées.");

        registry.Conclude(
            "Exécuté intégralement. Mode indisponible honnête, session isolée et réinitialisable, échec expliqué "
            + "sans fuite, attaque inter-base refusée, requête nominale validée, et porte fermée malgré des forces "
            + "C# réelles : la faiblesse SQL n'est compensée nulle part.");
    }

    private static async Task SolveAsync(PersonaSession session, string baseUrl, string exerciseId)
    {
        IPage page = session.Page;
        await session.GoAsync(baseUrl, $"/practice/{exerciseId}");
        await page.WaitForSelectorAsync("#reflection-reformulation");
        await page.FillAsync("#reflection-reformulation", $"Réussir {exerciseId} proprement pour constituer une force mesurée hors SQL.");
        await page.FillAsync("#reflection-inputs", "Les paramètres imposés par la signature publique du squelette.");
        await page.FillAsync("#reflection-output", "La valeur attendue par les exemples publics, sans effet de bord.");
        await page.FillAsync("#reflection-edges", "Bornes basses et hautes annoncées par les contraintes de l'énoncé.");
        await page.FillAsync("#reflection-hypothesis", "La solution directe suffit, la difficulté est dans les bornes.");
        await page.FillAsync("#reflection-plan", "Écrire, relire contre les exemples, puis prouver dans le conteneur isolé.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("text=Réflexion enregistrée");
        string solution = await File.ReadAllTextAsync(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "exercises", exerciseId, "solution", "Submission.cs"));
        await page.FillAsync("#attempt-submission", solution);
        await page.ClickAsync("button:has-text('Lancer compilation et tests')");
        await page.WaitForSelectorAsync("article.runner-result", new PageWaitForSelectorOptions { Timeout = 120_000 });
    }

    private static string ReadScenarioSolutionQuery()
    {
        string markdown = File.ReadAllText(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "sql", ScenarioId, "solution.md"));
        Match match = SqlBlockRegex().Match(markdown);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Requête de solution introuvable pour {ScenarioId}.");
        }

        return match.Groups[1].Value.Trim();
    }

    [GeneratedRegex("```sql\\s*(.*?)```", RegexOptions.Singleline)]
    private static partial Regex SqlBlockRegex();
}
