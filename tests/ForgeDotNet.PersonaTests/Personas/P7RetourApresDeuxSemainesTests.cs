using System.Text.Json;
using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P7 — Retour après deux semaines : plan accepté, note, signet, activité Practice et cartes
/// planifiées ; arrêt complet, translation déterministe de quatorze jours des horodatages persistés
/// (le produit n'expose volontairement aucune horloge réglable), redémarrage sur les mêmes données.
/// Rien ne se perd, rien ne culpabilise, l'échec repart à J+1 du jour réel.
/// </summary>
[Trait("Category", "Persona")]
public sealed class P7RetourApresDeuxSemainesTests
{
    private const string LessonId = "reference-types-001";
    private const string ExerciseId = "reference-total-001";

    [Fact]
    public async Task P7_RetourApresDeuxSemaines_ReprendSansPerteNiPenalite()
    {
        var registry = new EvidenceRegistry("p7-retour-quatorze-jours", "P7 — Retour après deux semaines");
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p7", PersonaRunnerMode.Docker);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        // État initial : profil, plan accepté, note, signet, activité Practice, cartes planifiées.
        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Sept");
        await page.FillAsync("#professional-goal", "Reprendre sereinement après une coupure de deux semaines.");
        await page.FillAsync("#weekly-hours", "8");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");

        await session.GoAsync(app.BaseUrl, "/diagnostic");
        await page.ClickAsync("button:has-text('diagnostic réduit')");
        await page.WaitForURLAsync("**/diagnostic/session/**");
        string sessionId = System.Text.RegularExpressions.Regex
            .Match(page.Url, "session/([0-9a-fA-F-]+)").Groups[1].Value;
        for (int section = 0; section < 3; section++)
        {
            ILocator startButton = page.Locator("button:has-text('Commencer cette section')");
            ILocator questions = page.Locator("fieldset.diagnostic-question");
            await Assertions.Expect(startButton.Or(questions).First).ToBeVisibleAsync(
                new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
            if (await startButton.CountAsync() > 0)
            {
                await startButton.ClickAsync();
            }

            await page.WaitForSelectorAsync("fieldset.diagnostic-question");
            int count = await page.Locator("fieldset.diagnostic-question").CountAsync();
            for (int question = 0; question < count; question++)
            {
                await page.ClickAsync($"fieldset.diagnostic-question >> nth={question} >> input[type=radio] >> nth=1");
                await page.WaitForTimeoutAsync(120);
            }

            await page.ClickAsync("button:has-text('Terminer cette section')");
            await page.WaitForSelectorAsync("text=Section terminée.");
        }

        await page.ClickAsync("button:has-text('Terminer le diagnostic')");
        await page.WaitForSelectorAsync("text=Terminée");
        // L'évaluation figée précède le plan : c'est elle que le plan référence.
        await session.GoAsync(app.BaseUrl, $"/diagnostic/session/{sessionId}/evaluation");
        await page.WaitForSelectorAsync("text=Intervalle d'incertitude");
        await session.GoAsync(app.BaseUrl, $"/plan/{sessionId}");
        await page.WaitForSelectorAsync("text=Charge proposée");
        await page.ClickAsync("button:has-text('Accepter cette version')");
        await page.WaitForSelectorAsync("text=acceptée et figée");

        await session.GoAsync(app.BaseUrl, $"/learn/{LessonId}");
        await page.ClickAsync("button:has-text('Ajouter un signet')");
        await page.WaitForSelectorAsync("button:has-text('Retirer le signet')");
        await page.FillAsync("#lesson-note", "Reprendre ici : relire la différence valeur/référence avant la suite.");
        await page.Locator("#lesson-note").BlurAsync();
        await page.WaitForTimeoutAsync(600);

        await SolveExerciseAsync(session, app.BaseUrl);
        await session.GoAsync(app.BaseUrl, "/reviews");
        await page.SelectOptionAsync("#personal-domain", "CSharp");
        await page.FillAsync("#personal-question", "Quelle est la différence entre un type valeur et un type référence en C# ?");
        await page.FillAsync("#personal-answer", "La copie : un type valeur se copie entièrement, un type référence copie la référence vers le même objet.");
        await page.ClickAsync("button:has-text('Planifier à J+1')");
        await page.WaitForSelectorAsync("text=carte(s) disponible(s)");
        await registry.CaptureAsync(page, "État initial constitué",
            "Plan accepté, signet et note posés, exercice réussi au runner (cartes planifiées), carte personnelle à J+1.");

        // Arrêt complet puis translation déterministe de quatorze jours.
        await app.StopAsync();
        int shifted = SqliteInspector.ShiftPersistedTimestamps(app.DatabasePath, TimeSpan.FromDays(14));
        registry.Note("Avance d'horloge simulée",
            $"Application arrêtée ; {shifted} horodatage(s) persistés reculés uniformément de quatorze jours. "
            + "Le produit n'expose aucune horloge de test : une horloge réglable serait un canal de falsification "
            + "de récence, la translation des données à l'arrêt est l'équivalent déterministe et documenté.");
        await app.RestartAsync(PersonaRunnerMode.Docker);

        // Reprise : rien n'est perdu, rien ne culpabilise.
        await session.GoAsync(app.BaseUrl, "/dashboard");
        string dashboard = await page.InnerTextAsync("main");
        Assert.DoesNotContain("série", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pénalité", dashboard, StringComparison.OrdinalIgnoreCase);
        await registry.CaptureAsync(page, "Dashboard au retour",
            "Les mesures restent factuelles : aucune série quotidienne, aucune culpabilisation.");

        await session.GoAsync(app.BaseUrl, $"/plan/{sessionId}");
        await page.WaitForSelectorAsync("text=Plan accepté");
        await session.GoAsync(app.BaseUrl, $"/learn/{LessonId}");
        await page.WaitForSelectorAsync("button:has-text('Retirer le signet')");
        string note = await page.InputValueAsync("#lesson-note");
        Assert.Contains("Reprendre ici", note, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "État restauré",
            "Plan toujours accepté, signet présent, note intacte après arrêt complet et redémarrage.");

        await session.GoAsync(app.BaseUrl, $"/practice/{ExerciseId}");
        await page.WaitForSelectorAsync("h4:has-text('Tentative 1')");
        registry.Note("Activité Practice restaurée", "L'historique de tentative et la réflexion figée sont intacts.");

        // Révisions : retard factuel, sans avalanche ni pénalité.
        await session.GoAsync(app.BaseUrl, "/reviews");
        string reviews = await page.InnerTextAsync("main");
        Assert.Contains("sans pénalité", reviews, StringComparison.Ordinal);
        Assert.Contains("disponible depuis", reviews, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Retard factuel sans dette",
            "Les cartes en retard affichent « disponible depuis N jour(s), sans pénalité » — aucune avalanche.");

        // Échec : replanifié à J+1 depuis le jour réel, jamais depuis l'ancienne échéance.
        // La file mêle cartes du bilan d'entrée, cartes d'exercice et carte personnelle ; seules les
        // cartes d'exercice ont leurs options dans la banque publiée que le harnais peut consulter.
        (ILocator choiceCard, string wrongOption, _) = await FindBankCardAsync(page);
        await choiceCard.Locator("select").SelectOptionAsync(wrongOption);
        await choiceCard.Locator("button:has-text('Vérifier')").ClickAsync();
        await page.WaitForSelectorAsync("text=Prochaine échéance");
        string failureResult = await page.InnerTextAsync("section[aria-live='polite']");
        Assert.Contains("À revoir", failureResult, StringComparison.Ordinal);
        string expectedTomorrow = DateTime.Now.AddDays(1).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(expectedTomorrow, failureResult, StringComparison.Ordinal);
        Assert.Contains("dans 1 jour", failureResult, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Échec replanifié à J+1 du jour réel",
            $"La carte ratée repart au {expectedTomorrow}, calculé depuis aujourd'hui et non depuis l'ancienne échéance.");

        // Réussite : avance au prochain intervalle documenté, daté depuis aujourd'hui.
        (ILocator secondCard, _, string correctOption) = await FindBankCardAsync(page);
        await secondCard.Locator("select").SelectOptionAsync(correctOption);
        await secondCard.Locator("button:has-text('Vérifier')").ClickAsync();
        await page.WaitForSelectorAsync("text=Réponse réussie");
        string successResult = await page.InnerTextAsync("section[aria-live='polite']");
        Assert.Contains("Prochaine échéance", successResult, StringComparison.Ordinal);
        Assert.DoesNotContain("dans 0 jour", successResult, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Réussite avancée au prochain intervalle",
            "La carte réussie avance à l'intervalle documenté suivant, daté du jour réel de la réponse.");

        // Maîtrise : preuves conservées et récence honnête (14 jours < fenêtre de 30).
        await session.GoAsync(app.BaseUrl, "/mastery");
        string mastery = await page.InnerTextAsync("main");
        Assert.Contains("observation(s) typée(s)", mastery, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Preuves et récence honnêtes",
            "Les observations persistées restent comptées ; quatorze jours n'expirent pas la fenêtre de récence de trente.");

        long attempts = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM PracticeAttempts");
        long reviewAttempts = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM ReviewAttempts");
        Assert.True(attempts >= 1, "L'historique de pratique doit avoir survécu au redémarrage.");
        Assert.True(reviewAttempts == 2, $"Deux réponses de révision attendues, trouvées {reviewAttempts}.");
        registry.Note("État persistant",
            "Tentatives de pratique conservées et deux réponses de révision enregistrées après la reprise.");

        registry.Conclude(
            "Exécuté intégralement, avance de quatorze jours simulée par translation déterministe des horodatages, "
            + "application arrêtée — limite du produit documentée : aucune horloge de test n'est exposée, par choix "
            + "d'intégrité. État restauré sans perte, retard factuel sans pénalité ni série, échec replanifié à J+1 "
            + "du jour réel, réussite avancée à l'intervalle documenté, récence honnête.");
    }

    private static async Task SolveExerciseAsync(PersonaSession session, string baseUrl)
    {
        IPage page = session.Page;
        await session.GoAsync(baseUrl, $"/practice/{ExerciseId}");
        await page.WaitForSelectorAsync("#reflection-reformulation");
        await page.FillAsync("#reflection-reformulation", "Additionner deux montants décimaux exactement, comme l'exige la signature.");
        await page.FillAsync("#reflection-inputs", "Deux montants décimaux fournis par les cas publics.");
        await page.FillAsync("#reflection-output", "Leur somme exacte sans arrondi binaire parasite.");
        await page.FillAsync("#reflection-edges", "Zéros, négatifs et échelles décimales longues aux bornes.");
        await page.FillAsync("#reflection-hypothesis", "L'addition du type décimal conserve l'échelle exacte attendue.");
        await page.FillAsync("#reflection-plan", "Écrire l'addition, vérifier chaque exemple public, prouver en conteneur.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("text=Réflexion enregistrée");
        string solution = await File.ReadAllTextAsync(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "exercises", ExerciseId, "solution", "Submission.cs"));
        await page.FillAsync("#attempt-submission", solution);
        await page.FillAsync("#attempt-verification", "Vérifié à la main sur les exemples publics : la somme rendue correspond exactement.");
        await page.CheckAsync("section[aria-labelledby='attempt-title'] input[type=checkbox]");
        await page.ClickAsync("button:has-text('Enregistrer la tentative')");
        await page.WaitForSelectorAsync("h4:has-text('Tentative 1')");
        await page.FillAsync("#attempt-submission", solution);
        await page.ClickAsync("button:has-text('Lancer compilation et tests')");
        await page.WaitForSelectorAsync("article.runner-result", new PageWaitForSelectorOptions { Timeout = 120_000 });
    }

    /// <summary>Première carte à choix affichée dont l'énoncé figure dans la banque publiée.</summary>
    private static async Task<(ILocator Card, string WrongOptionId, string CorrectOptionId)> FindBankCardAsync(IPage page)
    {
        ILocator cards = page.Locator("article.practice-panel:has(select[id^='choice-'])");
        int count = await cards.CountAsync();
        for (int index = 0; index < count; index++)
        {
            ILocator card = cards.Nth(index);
            string question = await card.Locator("p").First.InnerTextAsync();
            (string wrong, string correct)? options = TryReadCardOptionsByQuestion(question);
            if (options is not null)
            {
                return (card, options.Value.wrong, options.Value.correct);
            }
        }

        throw new InvalidOperationException("Aucune carte d'exercice de la banque publiée n'est due.");
    }

    /// <summary>Retrouve la carte affichée dans la banque publiée par son énoncé exact.</summary>
    private static (string WrongOptionId, string CorrectOptionId)? TryReadCardOptionsByQuestion(string question)
    {
        using JsonDocument bank = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "reviews", "exercise-review-cards.json")));
        foreach (JsonElement card in bank.RootElement.GetProperty("cards").EnumerateArray())
        {
            if (!string.Equals(card.GetProperty("question").GetString(), question.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            string correct = card.GetProperty("correctOptionId").GetString()!;
            string wrong = card.GetProperty("options").EnumerateArray()
                .Select(option => option.GetProperty("id").GetString()!)
                .First(id => id != correct);
            return (wrong, correct);
        }

        return null;
    }
}
