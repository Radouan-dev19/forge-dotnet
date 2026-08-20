using System.Globalization;
using System.Text.RegularExpressions;
using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P2 — Tricheur : chaque contournement est tenté avant les chemins nominaux. Quiz répétés bornés à
/// 5 %, déclarations manuelles à zéro, indices plafonnant à 60, variété de trois exercices exigée,
/// Practice verrouillée pendant l'examen, aucune porte ouverte par moyenne.
/// </summary>
[Trait("Category", "Persona")]
public sealed partial class P2TricheurTests
{
    private const string LessonId = "reference-types-001";
    private const string HintedExerciseId = "reference-total-001";

    [Fact]
    public async Task P2_Tricheur_NOuvreNiScoreNiPorteParContournement()
    {
        var registry = new EvidenceRegistry("p2-tricheur", "P2 — Tricheur");
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p2", PersonaRunnerMode.Docker);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Deux");
        await page.FillAsync("#professional-goal", "Ouvrir les portes avec le moins de travail vérifiable possible.");
        await page.FillAsync("#weekly-hours", "20");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");

        // Contournement 1 — quiz répétés : la composante reste bornée à 5 %.
        (int correctIndex, _) = QuizReader.Read(LessonId);
        await session.GoAsync(app.BaseUrl, $"/learn/{LessonId}");
        for (int repetition = 0; repetition < 3; repetition++)
        {
            await page.ClickAsync($"section.quiz-panel input[name=lesson-quiz] >> nth={correctIndex}");
            await page.ClickAsync("button:has-text('Vérifier ma réponse')");
            await page.WaitForSelectorAsync("section.quiz-panel p.success-message");
        }

        decimal quizOnlyScore = await ReadDomainScoreAsync(session, app.BaseUrl, "C#");
        Assert.True(quizOnlyScore <= 5m, $"Le quiz seul devrait plafonner à 5, score observé {quizOnlyScore}.");
        await registry.CaptureAsync(page, "Quiz répétés bornés",
            $"Trois réussites du même quiz laissent le domaine C# à {quizOnlyScore}/100 : la composante pèse 5 % au plus.");

        // Contournement 2 — déclaration manuelle : aucune preuve, score inchangé.
        await session.GoAsync(app.BaseUrl, $"/practice/{HintedExerciseId}");
        await page.FillAsync("#reflection-reformulation", "Je dois rendre la somme exacte de deux montants en type décimal.");
        await page.FillAsync("#reflection-inputs", "Deux montants décimaux imposés par la signature publique.");
        await page.FillAsync("#reflection-output", "Leur somme exacte, échelle décimale préservée.");
        await page.FillAsync("#reflection-edges", "Zéros, négatifs, grandes échelles décimales aux bornes de l'énoncé.");
        await page.FillAsync("#reflection-hypothesis", "L'addition native du type décimal suffit sans conversion binaire.");
        await page.FillAsync("#reflection-plan", "Poser l'addition, comparer aux exemples publics, déclarer honnêtement ce qui est vérifié.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("text=Réflexion enregistrée");
        await page.FillAsync("#attempt-submission",
            "// Je prétends avoir tout vérifié à la main pour voir si une déclaration suffit à marquer des points.");
        await page.FillAsync("#attempt-verification", "Je déclare une vérification complète sans l'avoir réellement faite.");
        await page.CheckAsync("section[aria-labelledby='attempt-title'] input[type=checkbox]");
        await page.ClickAsync("button:has-text('Enregistrer la tentative')");
        await page.WaitForSelectorAsync("h4:has-text('Tentative 1')");
        decimal afterDeclaration = await ReadDomainScoreAsync(session, app.BaseUrl, "C#");
        Assert.True(afterDeclaration == quizOnlyScore,
            $"Une déclaration manuelle ne doit rien changer : {quizOnlyScore} -> {afterDeclaration}.");
        registry.Note("Déclaration manuelle à zéro",
            $"Score C# identique avant/après la tentative déclarée ({afterDeclaration}/100) : une déclaration ne vaut aucune preuve.");

        // Contournement 3 — H1..H4 puis réussite : la preuve plafonne à 60.
        await session.GoAsync(app.BaseUrl, $"/practice/{HintedExerciseId}");
        for (int level = 1; level <= 4; level++)
        {
            await page.ClickAsync($"button:has-text('Débloquer H{level}')");
            await page.WaitForSelectorAsync($"h3:has-text('H{level} —')");
        }

        await registry.CaptureAsync(page, "Quatre indices consommés", "H1 à H4 tracés avant toute réussite.");
        string solution = await File.ReadAllTextAsync(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "exercises", HintedExerciseId, "solution", "Submission.cs"));
        await page.FillAsync("#attempt-submission", solution);
        await page.ClickAsync("button:has-text('Lancer compilation et tests')");
        await page.WaitForSelectorAsync("article.runner-result", new PageWaitForSelectorOptions { Timeout = 120_000 });
        string runnerText = await page.InnerTextAsync("article.runner-result");
        Assert.Contains("aucune maîtrise attribuée", runnerText, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Réussite après indices",
            "Le conteneur isolé valide le code, l'interface rappelle qu'aucune maîtrise n'est attribuée d'office.");

        decimal hintedScore = await ReadDomainScoreAsync(session, app.BaseUrl, "C#");
        string practiceRow = await ReadComponentRowAsync(session.Page, "C#", "Pratique autonome");
        Assert.Contains("60", practiceRow, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Plafond des indices",
            $"La pratique autonome affiche 60/100 après H1..H4 ; domaine C# à {hintedScore}/100, non validé.");

        // Contournement 4 — boucler sur le même exercice : la variété de trois items manque toujours.
        await session.GoAsync(app.BaseUrl, $"/practice/{HintedExerciseId}");
        for (int repetition = 0; repetition < 2; repetition++)
        {
            await page.FillAsync("#attempt-submission", solution);
            await page.ClickAsync("button:has-text('Lancer compilation et tests')");
            // L'historique volatil du serveur affiche déjà la première réussite au chargement.
            await page.WaitForSelectorAsync(
                $"article.runner-result >> nth={repetition + 1}",
                new PageWaitForSelectorOptions { Timeout = 120_000 });
        }

        string domainArticle = await ReadDomainArticleAsync(session, app.BaseUrl, "C#");
        Assert.Contains("non validée", domainArticle, StringComparison.Ordinal);
        Assert.Contains("Variété insuffisante : 1/3 items distincts.", domainArticle, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Répétition sans variété",
            "Trois exécutions du même exercice laissent un seul item distinct : le domaine reste non validé.");

        // Contournement 5 — la solution ne se donne pas au tricheur pressé.
        Assert.False(await page.IsVisibleAsync("button:has-text('Consulter la solution')"),
            "La solution ne doit pas être disponible sans deux tentatives sérieuses et le délai serveur.");
        registry.Note("Solution inaccessible au raccourci",
            "Aucun bouton de consultation : deux tentatives sérieuses et dix minutes de délai restent exigées.");

        // Contournements 6 et 7 — rejeu divergent et faux examen : aucun chemin d'interface n'existe.
        registry.Note("Rejeu d'identifiants divergent",
            "Aucune interface ne permet de resoumettre un contenu divergent sous le même identifiant ; la règle "
            + "serveur est couverte par les tests ExamIntegrity et Practice (57 règles adversariales vertes).");
        registry.Note("Faux examen ou faux livrable",
            "Aucune interface ne permet de déclarer un examen ou un livrable : seuls le serveur d'examen et les "
            + "suites d'acceptation produisent ces preuves.");

        // Contournement 8 — Practice pendant un examen actif.
        await session.GoAsync(app.BaseUrl, "/exams");
        await page.ClickAsync("article button:has-text('Démarrer l’examen') >> nth=0");
        await page.WaitForSelectorAsync("text=Les aides Forge.NET sont verrouillées");
        await registry.CaptureAsync(page, "Examen démarré", "L'examen actif verrouille les aides.");
        IPage secondTab = await page.Context.NewPageAsync();
        await secondTab.GotoAsync($"{app.BaseUrl}/practice/{HintedExerciseId}");
        await secondTab.WaitForSelectorAsync("text=verrouillés pendant l’examen");
        await registry.CaptureAsync(secondTab, "Practice verrouillée pendant l'examen",
            "L'accès direct à l'exercice répond « pratique, indices et solutions verrouillés pendant l'examen sans aide actif ».");
        await secondTab.CloseAsync();
        await page.ClickAsync("button:has-text('Abandonner')");
        await page.WaitForSelectorAsync("text=abandonné");

        // Bilan : aucune porte ouverte, aucun accomplissement, preuves cohérentes en base.
        await session.GoAsync(app.BaseUrl, "/dashboard");
        // L'état d'une porte vit dans son titre ; « Porte A ouverte » apparaît aussi comme LIBELLÉ
        // d'exigence bloquante de la porte B, ce qui n'est pas un état.
        IReadOnlyList<string> gateStates = await page
            .Locator("section[aria-labelledby='gates-title'] article h3")
            .AllInnerTextsAsync();
        Assert.True(gateStates.Count >= 4, "Les quatre portes doivent être listées.");
        Assert.All(gateStates, state => Assert.EndsWith("fermée", state));
        await registry.CaptureAsync(page, "Portes toutes fermées",
            "Aucune moyenne ni contournement n'a ouvert de porte : les quatre titres affichent « fermée ».");

        long succeededRuns = SqliteInspector.Scalar(
            app.DatabasePath, "SELECT COUNT(*) FROM PracticeLearningAttempts WHERE Status = 'Succeeded'");
        long distinctExercises = SqliteInspector.Scalar(
            app.DatabasePath, "SELECT COUNT(DISTINCT ExerciseId) FROM PracticeLearningAttempts WHERE Status = 'Succeeded'");
        long abandonedExams = SqliteInspector.Scalar(
            app.DatabasePath, "SELECT COUNT(*) FROM ExamAttempts WHERE Status LIKE '%bandon%' OR Status = 'Abandoned'");
        Assert.True(succeededRuns == 3, $"Trois exécutions réussies attendues, trouvées {succeededRuns}.");
        Assert.True(distinctExercises == 1, $"Un seul exercice distinct attendu, trouvé(s) {distinctExercises}.");
        Assert.True(abandonedExams == 1, $"Un examen abandonné attendu, trouvé(s) {abandonedExams}.");
        registry.Note("État persistant",
            "Trois réussites runner sur un seul exercice distinct, un examen abandonné, aucune preuve d'accomplissement.");

        registry.Conclude(
            "Exécuté intégralement, contournements tentés avant les chemins nominaux. Quiz borné à 5 %, déclaration "
            + "manuelle sans effet, plafond 60 après indices, variété exigée, solution retenue, Practice verrouillée "
            + "pendant l'examen, portes toutes fermées. Le rejeu divergent et le faux examen n'ont aucun chemin UI ; "
            + "leurs règles serveur restent couvertes par les tests adversariaux.");
    }

    private static async Task<decimal> ReadDomainScoreAsync(PersonaSession session, string baseUrl, string domainLabel)
    {
        string article = await ReadDomainArticleAsync(session, baseUrl, domainLabel);
        Match match = Regex.Match(article, $"{Regex.Escape(domainLabel)} — ([0-9]+(?:[.,][0-9]+)?) / 100");
        Assert.True(match.Success, $"Score du domaine {domainLabel} introuvable.");
        return decimal.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private static async Task<string> ReadDomainArticleAsync(PersonaSession session, string baseUrl, string domainLabel)
    {
        await session.GoAsync(baseUrl, "/mastery");
        ILocator article = session.Page.Locator($"article.practice-panel:has(h3:text-matches('^{Regex.Escape(domainLabel)} —'))");
        await Assertions.Expect(article).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 20_000 });
        return await article.InnerTextAsync();
    }

    private static async Task<string> ReadComponentRowAsync(IPage page, string domainLabel, string componentLabel)
    {
        ILocator row = page.Locator(
            $"article.practice-panel:has(h3:text-matches('^{Regex.Escape(domainLabel)} —')) tr:has-text('{componentLabel}')");
        return await row.InnerTextAsync();
    }
}
