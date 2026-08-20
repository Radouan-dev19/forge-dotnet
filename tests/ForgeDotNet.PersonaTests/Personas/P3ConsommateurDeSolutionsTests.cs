using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P3 — Consommateur de solutions : la solution ne s'obtient ni avant réflexion, ni sans deux
/// tentatives sérieuses distinctes, ni avant le délai serveur de dix minutes — attendu en temps
/// réel ici. Une fois consultée, l'exercice reste définitivement non maîtrisé, même après une
/// réussite ultérieure au runner Docker.
/// </summary>
[Trait("Category", "Persona")]
public sealed class P3ConsommateurDeSolutionsTests
{
    private const string ExerciseId = "reference-total-001";

    [Fact]
    public async Task P3_ConsommateurDeSolutions_NObtientJamaisDeMaitriseParLaSolution()
    {
        var registry = new EvidenceRegistry("p3-consommateur-solutions", "P3 — Consommateur de solutions");
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p3", PersonaRunnerMode.Docker);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Trois");
        await page.FillAsync("#professional-goal", "Obtenir les solutions le plus vite possible pour avancer.");
        await page.FillAsync("#weekly-hours", "10");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");

        // Avant toute réflexion : ni indice, ni solution.
        await session.GoAsync(app.BaseUrl, $"/practice/{ExerciseId}");
        Assert.False(await page.IsVisibleAsync("button:has-text('Débloquer H1')"),
            "Aucun indice ne doit être déblocable avant la réflexion.");
        Assert.False(await page.IsVisibleAsync("button:has-text('Consulter la solution')"),
            "La solution ne doit pas être proposée avant la réflexion.");
        string gate = await page.InnerTextAsync("section[aria-labelledby='solution-gate-title']");
        Assert.Contains("0 / 2", gate, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Accès prématuré refusé",
            "Ni bouton d'indice ni bouton de solution avant la réflexion ; jauge 0/2 tentative sérieuse.");

        // Réflexion complète, puis tentatives calibrées.
        await page.FillAsync("#reflection-reformulation", "Additionner deux montants décimaux sans les convertir en flottant binaire.");
        await page.FillAsync("#reflection-inputs", "Deux valeurs décimales fournies par les cas de test.");
        await page.FillAsync("#reflection-output", "La somme exacte des deux montants, sans arrondi parasite.");
        await page.FillAsync("#reflection-edges", "Montants nuls, négatifs et valeurs à nombreuses décimales significatives.");
        await page.FillAsync("#reflection-hypothesis", "L'opérateur d'addition du type decimal suffit à préserver l'échelle exacte.");
        await page.FillAsync("#reflection-plan", "Écrire l'addition directe, relire la signature imposée, vérifier les exemples de l'énoncé un par un.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("text=Réflexion enregistrée");

        // Tentative trop courte : disqualifiée.
        await page.FillAsync("#attempt-submission", "return a + b; // idée rapide");
        await page.FillAsync("#attempt-verification", "Aucune vérification sérieuse, je teste la barrière du produit.");
        await page.CheckAsync("section[aria-labelledby='attempt-title'] input[type=checkbox]");
        await page.ClickAsync("button:has-text('Enregistrer la tentative')");
        await page.WaitForSelectorAsync("text=Proposition trop courte");
        await registry.CaptureAsync(page, "Tentative trop courte disqualifiée",
            "La proposition brève est historisée « Proposition trop courte », pas sérieuse.");

        // Deux doublons : le premier passe s'il est sérieux, le second est marqué doublon.
        string duplicated = "public static class Submission { public static decimal AddAmounts(decimal a, decimal b) { return a; } } // brouillon incomplet à retravailler";
        foreach (int _ in Enumerable.Range(0, 2))
        {
            await page.FillAsync("#attempt-submission", duplicated);
            await page.FillAsync("#attempt-verification", "Relu les exemples de l'énoncé : le brouillon rend le premier montant, résultat faux.");
            await page.CheckAsync("section[aria-labelledby='attempt-title'] input[type=checkbox]");
            await page.ClickAsync("button:has-text('Enregistrer la tentative')");
            await page.WaitForSelectorAsync("text=Tentative enregistrée sans exécution automatique.");
        }

        await page.WaitForSelectorAsync("text=Doublon substantiel détecté");
        await registry.CaptureAsync(page, "Doublon non compté",
            "La resoumission du même texte est marquée « Doublon substantiel détecté » : une seule tentative sérieuse comptée.");

        // Seconde tentative sérieuse distincte (vocabulaire et structure volontairement différents).
        await page.FillAsync("#attempt-submission",
            "// Nouvelle piste après relecture posée de la signature imposée par le squelette fourni.\n"
            + "// Je détaille chaque étape pour comparer mon raisonnement aux exemples publics donnés.\n"
            + "public static class Submission\n{\n    public static decimal AddAmounts(decimal premier, decimal second)\n"
            + "    {\n        decimal resultat = premier - second; // étourderie d'opérateur conservée exprès\n"
            + "        return resultat;\n    }\n}");
        await page.FillAsync("#attempt-verification", "Vérifié à la main sur 2 + 3 : ma soustraction rend -1 au lieu de 5, c'est faux mais travaillé.");
        await page.CheckAsync("section[aria-labelledby='attempt-title'] input[type=checkbox]");
        await page.ClickAsync("button:has-text('Enregistrer la tentative')");
        await page.WaitForSelectorAsync("h4:has-text('Tentative 4')");
        gate = await page.InnerTextAsync("section[aria-labelledby='solution-gate-title']");
        Assert.Contains("2 / 2", gate, StringComparison.Ordinal);

        // Avant le délai serveur : la consultation reste refusée.
        Assert.False(await page.IsVisibleAsync("button:has-text('Consulter la solution')"),
            "Le délai serveur doit encore retenir la solution.");
        Assert.Contains("Délai serveur restant", gate, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Délai serveur conservé",
            "Deux tentatives sérieuses comptées et pourtant la solution reste retenue par le délai de dix minutes.");

        // Attente réelle du délai de dix minutes, en actualisant côté serveur.
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(13);
        while (!await page.IsVisibleAsync("button:has-text('Consulter la solution')"))
        {
            Assert.True(DateTime.UtcNow < deadline, "Le délai serveur de dix minutes n'a jamais expiré.");
            await page.WaitForTimeoutAsync(20_000);
            if (await page.IsVisibleAsync("button:has-text('Actualiser le délai')"))
            {
                await page.ClickAsync("button:has-text('Actualiser le délai')");
            }
        }

        await page.ClickAsync("button:has-text('Consulter la solution et marquer non maîtrisé')");
        await page.WaitForSelectorAsync("text=Solution consultée — activité non maîtrisée");
        string limits = await page.InnerTextAsync("section[aria-labelledby='limits-title']");
        Assert.Contains("carte de", limits, StringComparison.Ordinal);
        Assert.Contains("récupération", limits, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Solution consultée après délai",
            "État « Solution consultée — activité non maîtrisée » et annonce de la carte de récupération planifiée.");

        // Explication superficielle refusée, puis reprise causale acceptée.
        await page.FillAsync("#personal-explanation", "C'est simple.");
        await page.FillAsync("#variant-submission", "Pareil.");
        await page.ClickAsync("button:has-text('Enregistrer la reprise non maîtrisée')");
        await page.WaitForSelectorAsync("p.error-message");
        await registry.CaptureAsync(page, "Explication superficielle refusée",
            "La reprise trop courte est rejetée par le serveur avec le minimum exigé.");

        await page.FillAsync("#personal-explanation",
            "Le type decimal conserve une échelle exacte en base dix : additionner deux montants avec lui "
            + "n'introduit aucun arrondi binaire, là où double aurait déformé certains centimes. Ma soustraction "
            + "était une étourderie d'opérateur, pas un problème de représentation.");
        await page.FillAsync("#variant-submission",
            "Pour la variante, je poserais la même addition sur des quantités entières en int, en vérifiant "
            + "d'abord les bornes annoncées par l'énoncé pour éviter tout débordement silencieux avant l'accumulation.");
        await page.ClickAsync("button:has-text('Enregistrer la reprise non maîtrisée')");
        await page.WaitForSelectorAsync("text=Reprise renseignée");
        await registry.CaptureAsync(page, "Reprise causale acceptée",
            "Explication et variante distinctes enregistrées, toujours sans maîtrise.");

        // Reviews : la carte de récupération est réellement planifiée.
        await session.GoAsync(app.BaseUrl, "/reviews");
        string reviews = await page.InnerTextAsync("main");
        Assert.Contains(ExerciseId, reviews, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Carte de récupération planifiée",
            "L'exercice contaminé apparaît dans le calendrier de révisions.");

        // Réussite ultérieure au runner : les tests passent, la maîtrise reste à zéro.
        string solution = await File.ReadAllTextAsync(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "exercises", ExerciseId, "solution", "Submission.cs"));
        await session.GoAsync(app.BaseUrl, $"/practice/{ExerciseId}");
        Assert.False(await page.IsVisibleAsync("#attempt-submission"),
            "Après consultation, l'activité ne doit plus accepter de nouvelles tentatives comptées.");
        registry.Note("Réussite ultérieure sur la même activité",
            "L'interface n'offre plus de tentative sur l'activité contaminée : la contamination est définitive "
            + "dans la politique v1 — le runner ne peut plus être invoqué depuis cette activité.");

        // État persistant : observation SolutionViewed dans Reviews, aucune pratique autonome admissible.
        long recoveryItems = SqliteInspector.Scalar(
            app.DatabasePath,
            $"SELECT COUNT(*) FROM ReviewItems WHERE SourceItemId LIKE '%{ExerciseId}%' OR SourceKey LIKE '%{ExerciseId}%'");
        long provenRuns = SqliteInspector.Scalar(
            app.DatabasePath, "SELECT COUNT(*) FROM PracticeLearningAttempts WHERE Status = 'Succeeded'");
        Assert.True(recoveryItems >= 1, $"Carte de récupération attendue en base, trouvée(s) {recoveryItems}.");
        Assert.True(provenRuns == 0, $"Aucune réussite runner attendue, trouvée(s) {provenRuns}.");
        registry.Note("État persistant",
            $"ReviewItems de récupération = {recoveryItems} ; PracticeLearningAttempts réussies = 0 : "
            + "aucune maîtrise immédiate après la solution.");

        // Maîtrise : la pratique autonome du domaine reste sans preuve.
        await session.GoAsync(app.BaseUrl, "/mastery");
        string mastery = await page.InnerTextAsync("main");
        Assert.Contains("absente — 0", mastery, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Maîtrise inchangée",
            "Les composantes restent sans preuve : la consultation n'a rien ouvert.");

        registry.Conclude(
            "Exécuté intégralement, délai serveur de dix minutes attendu en temps réel. Accès prématuré refusé, "
            + "doublons disqualifiés, délai conservé, contamination définitive annoncée et persistée, explication "
            + "superficielle refusée, carte de récupération planifiée, aucune maîtrise créée.");
    }
}
