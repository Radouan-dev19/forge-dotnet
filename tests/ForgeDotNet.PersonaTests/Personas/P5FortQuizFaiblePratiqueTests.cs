using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P5 — Fort quiz, faible pratique : lecture et quiz brillants, deux réussites automatiques dont une
/// assistée, ni troisième exercice ni examen. Aucun libellé « prêt », composante quiz bornée,
/// variété et récence explicitement bloquantes, portes fermées.
/// </summary>
[Trait("Category", "Persona")]
public sealed class P5FortQuizFaiblePratiqueTests
{
    private static readonly string[] LessonIds =
        ["reference-types-001", "csharp-control-methods-001", "csharp-io-debugger-001"];

    private const string CleanExerciseId = "reference-total-001";
    private const string AssistedExerciseId = "algo-binary-search-001";

    [Fact]
    public async Task P5_FortQuizFaiblePratique_NeRecoitAucunLibellePret()
    {
        var registry = new EvidenceRegistry("p5-fort-quiz", "P5 — Fort quiz, faible pratique");
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p5", PersonaRunnerMode.Docker);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Cinq");
        await page.FillAsync("#professional-goal", "Briller aux quiz de lecture sans pratiquer sans aide.");
        await page.FillAsync("#weekly-hours", "12");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");

        // Lecture complète d'une leçon et réussite de trois quiz.
        foreach (string lessonId in LessonIds)
        {
            (int correctIndex, _) = QuizReader.Read(lessonId);
            await session.GoAsync(app.BaseUrl, $"/learn/{lessonId}");
            if (lessonId == LessonIds[0])
            {
                // La première leçon est lue intégralement : chaque section confirmée.
                while (await page.Locator("button:has-text('Marquer cette section comme lue')").CountAsync() > 0)
                {
                    await page.ClickAsync("button:has-text('Marquer cette section comme lue') >> nth=0");
                    await page.WaitForTimeoutAsync(150);
                }
            }

            await page.ClickAsync($"section.quiz-panel input[name=lesson-quiz] >> nth={correctIndex}");
            await page.ClickAsync("button:has-text('Vérifier ma réponse')");
            await page.WaitForSelectorAsync("section.quiz-panel p.success-message");
        }

        await registry.CaptureAsync(page, "Lecture et quiz brillants",
            "Une leçon lue à 100 % et trois quiz réussis : le profil de lecteur fort est constitué.");

        // Première réussite automatique, propre.
        await SolveAsync(session, app.BaseUrl, CleanExerciseId, unlockFirstHint: false);
        // Seconde réussite, assistée d'un indice.
        await SolveAsync(session, app.BaseUrl, AssistedExerciseId, unlockFirstHint: true);
        await registry.CaptureAsync(page, "Deux réussites automatiques",
            "Deux exercices validés en conteneur isolé, dont un assisté par H1. Aucun troisième exercice, aucun examen.");

        // Maîtrise : lecture distincte de maîtrise, composantes manquantes à zéro, blocages explicites.
        await session.GoAsync(app.BaseUrl, "/mastery");
        ILocator domainArticle = page.Locator("article.practice-panel:has(h3:text-matches('^C# —'))");
        string article = await domainArticle.InnerTextAsync();
        Assert.Contains("non validée", article, StringComparison.Ordinal);
        Assert.Contains("Variété insuffisante : 2/3 items distincts.", article, StringComparison.Ordinal);
        ILocator examRow = domainArticle.Locator("tr:has-text('Examen sans aide')");
        Assert.Contains("absente — 0", await examRow.InnerTextAsync(), StringComparison.Ordinal);
        ILocator retentionRow = domainArticle.Locator("tr:has-text('Rétention espacée')");
        Assert.Contains("absente — 0", await retentionRow.InnerTextAsync(), StringComparison.Ordinal);
        string masteryPage = await page.InnerTextAsync("main");
        Assert.DoesNotContain("prêt", masteryPage, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Quiz fort, maîtrise honnête",
            "Examen et rétention à zéro, variété 2/3 bloquante, aucun libellé « prêt ».");

        // Dashboard et portes : forces fondées sur les observations réelles, tout reste fermé.
        await session.GoAsync(app.BaseUrl, "/dashboard");
        IReadOnlyList<string> gateStates = await page
            .Locator("section[aria-labelledby='gates-title'] article h3")
            .AllInnerTextsAsync();
        Assert.True(gateStates.Count >= 4, "Les quatre portes doivent être listées.");
        Assert.All(gateStates, state => Assert.EndsWith("fermée", state));
        await registry.CaptureAsync(page, "Portes fermées malgré les quiz",
            "Les quatre portes restent fermées : la complétion de cours ne se confond pas avec l'autonomie.");

        // Récence : les preuves vieillissent de 31 jours, application arrêtée, puis redémarrage.
        await app.StopAsync();
        int shifted = SqliteInspector.ShiftPersistedTimestamps(app.DatabasePath, TimeSpan.FromDays(31));
        registry.Note("Vieillissement déterministe des preuves",
            $"Application arrêtée, {shifted} horodatage(s) persistés reculés de 31 jours — le produit n'expose "
            + "volontairement aucune horloge réglable, qui serait un canal de falsification de récence.");
        await app.RestartAsync(PersonaRunnerMode.Docker);
        await session.GoAsync(app.BaseUrl, "/mastery");
        string agedArticle = await page
            .Locator("article.practice-panel:has(h3:text-matches('^C# —'))")
            .InnerTextAsync();
        Assert.Contains("Aucune preuve récente, vérifiée et sans aide sur 30 jours.", agedArticle, StringComparison.Ordinal);
        Assert.Contains("non validée", agedArticle, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Récence expirée et annoncée",
            "Après 31 jours simulés, le blocage de récence est explicite et le domaine reste non validé.");

        long succeeded = SqliteInspector.Scalar(
            app.DatabasePath, "SELECT COUNT(DISTINCT ExerciseId) FROM PracticeLearningAttempts WHERE Status = 'Succeeded'");
        long exams = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM ExamAttempts");
        Assert.True(succeeded == 2, $"Deux exercices réussis distincts attendus, trouvés {succeeded}.");
        Assert.True(exams == 0, $"Aucun examen attendu, trouvé(s) {exams}.");
        registry.Note("État persistant",
            "Deux exercices réussis distincts, zéro examen : le profil scripté est exactement celui mesuré.");

        registry.Conclude(
            "Exécuté intégralement. La lecture et les quiz ne fabriquent aucune préparation : composantes absentes "
            + "à zéro, variété 2/3 et récence explicitement bloquantes après vieillissement déterministe, portes fermées, "
            + "aucun libellé « prêt » nulle part.");
    }

    private static async Task SolveAsync(PersonaSession session, string baseUrl, string exerciseId, bool unlockFirstHint)
    {
        IPage page = session.Page;
        await session.GoAsync(baseUrl, $"/practice/{exerciseId}");
        await page.WaitForSelectorAsync("#reflection-reformulation");
        await page.FillAsync("#reflection-reformulation", $"Résoudre {exerciseId} en respectant strictement la signature imposée par le squelette.");
        await page.FillAsync("#reflection-inputs", "Les paramètres décrits par la signature publique de l'exercice.");
        await page.FillAsync("#reflection-output", "La valeur exacte attendue par les exemples publics de l'énoncé.");
        await page.FillAsync("#reflection-edges", "Les bornes annoncées : vides, extrêmes, valeurs répétées ou absentes.");
        await page.FillAsync("#reflection-hypothesis", "L'algorithme direct décrit par l'énoncé suffit sans structure auxiliaire.");
        await page.FillAsync("#reflection-plan", "Écrire la solution, la relire contre chaque exemple public, puis lancer les tests isolés.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("text=Réflexion enregistrée");
        if (unlockFirstHint)
        {
            await page.ClickAsync("button:has-text('Débloquer H1')");
            await page.WaitForSelectorAsync("h3:has-text('H1 —')");
        }

        string solution = await File.ReadAllTextAsync(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "exercises", exerciseId, "solution", "Submission.cs"));
        await page.FillAsync("#attempt-submission", solution);
        await page.ClickAsync("button:has-text('Lancer compilation et tests')");
        await page.WaitForSelectorAsync("article.runner-result", new PageWaitForSelectorOptions { Timeout = 120_000 });
        string result = await page.InnerTextAsync("article.runner-result >> nth=0");
        Assert.Contains("aucune maîtrise attribuée", result, StringComparison.Ordinal);
    }
}
