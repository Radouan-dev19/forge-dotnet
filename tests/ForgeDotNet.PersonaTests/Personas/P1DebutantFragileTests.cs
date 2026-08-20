using System.Text.RegularExpressions;
using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P1 — Débutant fragile : 6 h disponibles, diagnostic réduit volontairement pauvre. Le produit ne
/// doit ni affirmer une maîtrise, ni imposer une charge impossible, ni compter un quiz raté, ni
/// accepter une réflexion vague sans aide actionnable.
/// </summary>
[Trait("Category", "Persona")]
public sealed partial class P1DebutantFragileTests
{
    private const string LessonId = "reference-types-001";
    private const string ExerciseId = "reference-total-001";

    [Fact]
    public async Task P1_DebutantFragile_ResteProtegeParLaPrudenceDuProduit()
    {
        var registry = new EvidenceRegistry("p1-debutant-fragile", "P1 — Débutant fragile");
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p1", PersonaRunnerMode.Manual);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        // Profil : 6 h, contrat accepté.
        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Une");
        await page.FillAsync("#professional-goal", "Se reconvertir vers le développement .NET avec six heures par semaine.");
        await page.FillAsync("#weekly-hours", "6");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");
        await registry.CaptureAsync(page, "Profil 6 h et contrat accepté", "Le profil fictif est persisté.");

        // Diagnostic réduit, réponses volontairement pauvres : une seule réponse par section.
        await session.GoAsync(app.BaseUrl, "/diagnostic");
        await page.ClickAsync("button:has-text('diagnostic réduit')");
        try
        {
            await page.WaitForURLAsync("**/diagnostic/session/**");
        }
        catch (TimeoutException)
        {
            await registry.CaptureAsync(page, "DIAGNOSTIC BLOQUÉ", "Capture d'investigation.");
            string state = await page.InnerTextAsync("main");
            Assert.Fail($"Le démarrage du diagnostic n'a pas navigué. Page : {state[..Math.Min(600, state.Length)]}");
        }
        string sessionUrl = page.Url;
        string sessionId = Regex.Match(sessionUrl, "session/([0-9a-fA-F-]+)").Groups[1].Value;

        for (int section = 0; section < 3; section++)
        {
            // La première section démarre avec la session ; les suivantes exigent l'action explicite.
            ILocator startButton = page.Locator("button:has-text('Commencer cette section')");
            ILocator questions = page.Locator("fieldset.diagnostic-question");
            await Assertions.Expect(startButton.Or(questions).First).ToBeVisibleAsync(
                new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
            if (await startButton.CountAsync() > 0)
            {
                await startButton.ClickAsync();
            }

            await page.WaitForSelectorAsync("fieldset.diagnostic-question");
            // Une seule réponse (la première option, très probablement fausse), les autres omises.
            await page.ClickAsync("fieldset.diagnostic-question >> nth=0 >> input[type=radio] >> nth=0");
            await page.WaitForSelectorAsync("text=Réponse enregistrée localement.");
            await page.ClickAsync("button:has-text('Terminer cette section')");
            await page.WaitForSelectorAsync("text=Section terminée.");
        }

        await page.WaitForSelectorAsync("text=La session sera honnêtement marquée comme incomplète.");
        await registry.CaptureAsync(page, "Collecte volontairement incomplète",
            "Le produit annonce avant clôture que la session sera marquée incomplète.");
        await page.ClickAsync("button:has-text('Terminer le diagnostic')");
        await page.WaitForSelectorAsync("text=Terminée — collecte incomplète");
        await registry.CaptureAsync(page, "Diagnostic clos", "Statut « Terminée — collecte incomplète » affiché.");

        // Évaluation : prudence, incertitude, lacunes non compensées.
        await session.GoAsync(app.BaseUrl, $"/diagnostic/session/{sessionId}/evaluation");
        await page.WaitForSelectorAsync("text=Intervalle d'incertitude");
        string evaluation = await page.InnerTextAsync("main");
        Assert.Contains("Intervalle d'incertitude", evaluation, StringComparison.Ordinal);
        Assert.True(
            evaluation.Contains("preuves insuffisantes", StringComparison.Ordinal)
            || evaluation.Contains("fondamentaux à renforcer", StringComparison.Ordinal),
            "Le niveau affiché doit rester prudent après une collecte pauvre.");
        Assert.Contains("Rapport provisoire", evaluation, StringComparison.Ordinal);
        Assert.Contains("Lacunes critiques", evaluation, StringComparison.Ordinal);
        Assert.DoesNotContain("maîtrisé", evaluation, StringComparison.OrdinalIgnoreCase);
        await registry.CaptureAsync(page, "Évaluation prudente",
            "Niveau prudent, intervalle visible, rapport provisoire, lacunes critiques listées, aucune maîtrise affirmée.");

        // Plan : charge hors bornes refusée, charge valide acceptée, contrôle conservé.
        await session.GoAsync(app.BaseUrl, $"/plan/{sessionId}");
        await page.WaitForSelectorAsync("text=Charge proposée");
        await page.FillAsync("#target-hours", "99");
        await page.ClickAsync("button:has-text('Créer une nouvelle version')");
        bool clientSideBlocked = !await page.EvalOnSelectorAsync<bool>("#target-hours", "input => input.checkValidity()");
        Assert.True(clientSideBlocked, "La validation native du champ devrait refuser 99 h.");
        await registry.CaptureAsync(page, "Charge 99 h bloquée par le champ",
            "La validation native (max=6) empêche même la soumission du formulaire.");

        // Contournement du garde-fou client pour prouver le refus serveur à travers l'interface.
        await page.EvalOnSelectorAsync("#target-hours", "input => input.removeAttribute('max')");
        await page.FillAsync("#target-hours", "99");
        await page.ClickAsync("button:has-text('Créer une nouvelle version')");
        await page.WaitForSelectorAsync("p.error-message");
        await registry.CaptureAsync(page, "Charge hors bornes refusée",
            "Le serveur refuse 99 h : l'erreur est affichée, aucune version créée avec cette charge.");

        await page.FillAsync("#target-hours", "5");
        await page.ClickAsync("button:has-text('Créer une nouvelle version')");
        await page.WaitForSelectorAsync("text=créée avec une charge de 5 h");
        await page.ClickAsync("button:has-text('Accepter cette version')");
        await page.WaitForSelectorAsync("text=acceptée et figée");
        await session.GoAsync(app.BaseUrl, $"/plan/{sessionId}");
        await page.WaitForSelectorAsync("text=Plan accepté");
        string plan = await page.InnerTextAsync("main");
        Assert.Contains("5 h/semaine", plan, StringComparison.Ordinal);
        Assert.Contains("Contrôle conservé", plan, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Plan accepté et relu",
            "Version figée à 5 h/semaine, compatible avec 6 h disponibles ; le contrôle conservé reste visible.");

        // Leçon S1 : échec de quiz sans progression, puis réussite.
        (int correctIndex, int optionCount) = ReadQuizAnswer();
        int wrongIndex = correctIndex == 0 ? 1 : 0;
        Assert.True(optionCount >= 3, "Le quiz de la leçon modèle doit offrir au moins trois options.");
        await session.GoAsync(app.BaseUrl, $"/learn/{LessonId}");
        await page.WaitForSelectorAsync("section.quiz-panel");
        string progressBefore = await page.InnerTextAsync("section.reading-progress");
        await page.ClickAsync($"section.quiz-panel input[name=lesson-quiz] >> nth={wrongIndex}");
        await page.ClickAsync("button:has-text('Vérifier ma réponse')");
        await page.WaitForSelectorAsync("section.quiz-panel p.notice");
        string progressAfterFailure = await page.InnerTextAsync("section.reading-progress");
        Assert.Equal(progressBefore, progressAfterFailure);
        await registry.CaptureAsync(page, "Quiz raté sans progression",
            "La réponse fausse reçoit un retour sans faire progresser la lecture.");

        await page.ClickAsync($"section.quiz-panel input[name=lesson-quiz] >> nth={correctIndex}");
        await page.ClickAsync("button:has-text('Vérifier ma réponse')");
        await page.WaitForSelectorAsync("section.quiz-panel p.success-message");
        await registry.CaptureAsync(page, "Quiz réussi après échec",
            "La bonne réponse est expliquée ; seule cette réussite compte dans la progression.");

        // Pratique : réflexion volontairement vague refusée avec aide actionnable.
        await session.GoAsync(app.BaseUrl, $"/practice/{ExerciseId}");
        await page.FillAsync("#reflection-reformulation", "Un total.");
        await page.FillAsync("#reflection-inputs", "Des nombres.");
        await page.FillAsync("#reflection-output", "Un nombre.");
        await page.FillAsync("#reflection-edges", "Aucun.");
        await page.FillAsync("#reflection-hypothesis", "Facile.");
        await page.FillAsync("#reflection-plan", "Coder.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("p.error-message");
        string practice = await page.InnerTextAsync("main");
        Assert.Contains("caractères minimum", practice, StringComparison.Ordinal);
        Assert.True(await page.IsVisibleAsync("#reflection-reformulation"), "Le formulaire doit rester modifiable.");
        await registry.CaptureAsync(page, "Réflexion vague refusée",
            "Le serveur refuse la réflexion insuffisante ; les minima par champ restent affichés comme aide.");

        // État persistant : session diagnostique incomplète et plan accepté, aucune preuve de maîtrise.
        long sessions = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM DiagnosticSessions");
        long acceptedPlans = SqliteInspector.Scalar(
            app.DatabasePath, "SELECT COUNT(*) FROM WeeklyPlans WHERE AcceptedAtUtc IS NOT NULL");
        long provenRuns = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM PracticeLearningAttempts");
        Assert.True(sessions == 1, $"Une session diagnostique attendue, trouvée(s) {sessions}.");
        Assert.True(acceptedPlans == 1, $"Un plan accepté attendu, trouvé(s) {acceptedPlans}.");
        Assert.True(provenRuns == 0, $"Aucune observation runner attendue, trouvée(s) {provenRuns}.");
        registry.Note("État persistant",
            "DiagnosticSessions=1 (incomplète), WeeklyPlans acceptés=1, PracticeLearningAttempts=0 : "
            + "aucune maîtrise n'a été fabriquée par ce parcours fragile.");

        registry.Conclude(
            "Exécuté intégralement. Niveau prudent avec incertitude visible, lacunes non compensées, charge "
            + "hors bornes refusée puis plan 5 h accepté et relu, quiz raté sans progression puis réussi, "
            + "réflexion vague refusée avec les minima affichés. Aucun faux statut de maîtrise.");
    }

    /// <summary>Lit l'index de la bonne réponse du quiz dans le contenu publié (le harnais est l'auteur du test).</summary>
    private static (int CorrectIndex, int OptionCount) ReadQuizAnswer()
    {
        string markdown = File.ReadAllText(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "curriculum", "lessons", LessonId, "lesson.md"));
        Match quiz = QuizRegex().Match(markdown);
        Assert.True(quiz.Success, "Bloc quiz introuvable dans la leçon modèle.");
        string block = quiz.Value;
        int options = Regex.Matches(block, "^option=", RegexOptions.Multiline).Count;
        Match correct = Regex.Match(block, "^correct=(\\d+)", RegexOptions.Multiline);
        Assert.True(correct.Success, "Index de bonne réponse introuvable dans le quiz.");
        return (int.Parse(correct.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), options);
    }

    [GeneratedRegex(":::quiz.*?:::", RegexOptions.Singleline)]
    private static partial Regex QuizRegex();
}
