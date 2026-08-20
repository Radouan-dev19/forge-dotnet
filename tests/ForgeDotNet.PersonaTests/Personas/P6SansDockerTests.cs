using System.IO.Compression;
using ForgeDotNet.PersonaTests.Harness;
using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Personas;

/// <summary>
/// P6 — Sans Docker : CodeRunner en mode manuel, SqlLab désactivé. Le produit doit rester
/// utilisable en consultation, expliquer chaque indisponibilité, n'exporter que du contenu public
/// et n'attribuer aucune réussite, tentative sérieuse automatique ni maîtrise au mode manuel.
/// </summary>
[Trait("Category", "Persona")]
public sealed class P6SansDockerTests
{
    private const string ExerciseId = "reference-total-001";

    [Fact]
    public async Task P6_SansDocker_NavigueExporteEtNeProduitAucunePreuve()
    {
        var registry = new EvidenceRegistry("p6-sans-docker", "P6 — Sans Docker");
        await using ForgeAppInstance app = await ForgeAppInstance.StartAsync("p6", PersonaRunnerMode.Manual);
        await using PersonaSession session = await PersonaSession.LaunchAsync();
        IPage page = session.Page;

        // Profil fictif minimal : l'état initial du script.
        await session.GoAsync(app.BaseUrl, "/profile");
        await page.FillAsync("#display-name", "Persona Six");
        await page.FillAsync("#professional-goal", "Explorer le produit sans moteur Docker disponible.");
        await page.FillAsync("#weekly-hours", "5");
        await page.CheckAsync("#learning-contract");
        await page.ClickAsync("button:has-text('Enregistrer le profil')");
        await page.WaitForSelectorAsync("text=Profil enregistré dans la base locale.");
        await registry.CaptureAsync(page, "Profil fictif créé", "Statut « Profil enregistré » affiché.");

        // Parcours consultatif : accueil, leçon, DebugLab, examens, dashboard.
        await session.GoAsync(app.BaseUrl, "/");
        await registry.CaptureAsync(page, "Accueil", "La page d'accueil est servie et navigable.");
        await session.GoAsync(app.BaseUrl, "/learn");
        await page.ClickAsync("ul.lesson-list a >> nth=0");
        await page.WaitForSelectorAsync("h1");
        await registry.CaptureAsync(page, "Leçon ouverte", "Une leçon S1 se lit intégralement sans runner.");

        // Practice en mode manuel : réflexion, tentative déclarée, exécution demandée.
        await session.GoAsync(app.BaseUrl, $"/practice/{ExerciseId}");
        Assert.Contains("mode manuel", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
        await page.FillAsync("#reflection-reformulation", "Je dois calculer un total à partir des éléments fournis par l'énoncé.");
        await page.FillAsync("#reflection-inputs", "Les valeurs d'entrée décrites par l'énoncé.");
        await page.FillAsync("#reflection-output", "Le total attendu par les exemples.");
        await page.FillAsync("#reflection-edges", "Les cas vides, les valeurs extrêmes et les bornes annoncées.");
        await page.FillAsync("#reflection-hypothesis", "Un parcours simple des entrées suffit pour accumuler le total.");
        await page.FillAsync("#reflection-plan", "Lire l'énoncé, écrire l'accumulation, vérifier les exemples à la main avant de soumettre.");
        await page.ClickAsync("button:has-text('Enregistrer et figer au premier acte')");
        await page.WaitForSelectorAsync("text=Réflexion enregistrée");
        await registry.CaptureAsync(page, "Réflexion enregistrée", "Les six champs sont acceptés par le serveur.");

        await page.FillAsync("#attempt-verification", "Relu les exemples de l'énoncé à la main ; résultat conforme sur les cas visibles.");
        await page.CheckAsync("section[aria-labelledby='attempt-title'] input[type=checkbox]");
        await page.ClickAsync("button:has-text('Enregistrer la tentative')");
        await page.WaitForSelectorAsync("text=Tentative enregistrée sans exécution automatique.");
        await registry.CaptureAsync(page, "Tentative manuelle déclarée", "La tentative est enregistrée comme déclarée, sans exécution.");

        // Demande d'exécution : le mode manuel doit répondre « indisponible », jamais « réussi ».
        await page.FillAsync("#attempt-submission", "public static class Submission { }");
        await page.ClickAsync("button:has-text('Lancer compilation et tests')");
        await page.WaitForSelectorAsync("article.runner-result", new PageWaitForSelectorOptions { Timeout = 60_000 });
        string runnerText = await page.InnerTextAsync("article.runner-result");
        Assert.Contains("Runner indisponible", runnerText, StringComparison.Ordinal);
        Assert.Contains("transmis à Docker", runnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("réussis", runnerText, StringComparison.Ordinal);
        await registry.CaptureAsync(page, "Exécution demandée en mode manuel",
            "Le résultat affiche « Runner indisponible » et « aucun code n’a été transmis à Docker », aucun test réussi.");

        // Export ZIP : contenu public seulement — aucun chemin de tests cachés ni de solution.
        Task<IDownload> downloadTask = page.WaitForDownloadAsync();
        await page.ClickAsync("button:has-text('Exporter le ZIP manuel')");
        IDownload download = await downloadTask;
        string zipPath = Path.Combine(registry.Directory1, download.SuggestedFilename);
        await download.SaveAsAsync(zipPath);
        using (ZipArchive zip = ZipFile.OpenRead(zipPath))
        {
            string[] entries = zip.Entries.Select(entry => entry.FullName).ToArray();
            Assert.DoesNotContain(entries, entry => entry.Contains("hidden", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entries, entry => entry.Contains("solution", StringComparison.OrdinalIgnoreCase));
            registry.Note("Export ZIP inspecté",
                $"{entries.Length} entrée(s) : {string.Join(", ", entries)} — ni tests cachés ni solution.");
        }

        await registry.CaptureAsync(page, "Export ZIP manuel",
            "Le statut annonce que Forge.NET n'a ni exécuté ni validé le contenu exporté.");

        // SqlLab désactivé : indisponibilité honnête, aucune validation affirmée.
        await session.GoAsync(app.BaseUrl, "/sql-lab");
        await page.WaitForSelectorAsync("text=SqlLab reste indisponible");
        await registry.CaptureAsync(page, "SqlLab désactivé",
            "Le message annonce l'indisponibilité sans affirmer de validation automatique.");

        // DebugLab et examens restent consultables.
        await session.GoAsync(app.BaseUrl, "/debug-lab");
        await registry.CaptureAsync(page, "DebugLab consultable", "La liste des scénarios est servie.");
        await session.GoAsync(app.BaseUrl, "/exams");
        await registry.CaptureAsync(page, "Examens consultables", "La banque d'examens est listée sans démarrage.");

        // Maîtrise : la bannière d'installation non probante et zéro preuve.
        await session.GoAsync(app.BaseUrl, "/mastery");
        await page.WaitForSelectorAsync("text=Cette installation ne peut produire aucune preuve.");
        await registry.CaptureAsync(page, "Maîtrise en mode manuel",
            "La bannière distingue une installation incapable de prouver d'un manque de travail.");

        await session.GoAsync(app.BaseUrl, "/dashboard");
        await registry.CaptureAsync(page, "Dashboard", "Les mesures restent indisponibles ou factuelles, sans faux signal.");

        // État persistant : la demande d'exécution est tracée « Unavailable », zéro test rapporté —
        // c'est un enregistrement honnête, pas une preuve. Aucune réussite, aucun test exécuté.
        long provenRuns = SqliteInspector.Scalar(
            app.DatabasePath,
            "SELECT COUNT(*) FROM PracticeLearningAttempts WHERE Status = 'Succeeded' OR TotalTests > 0");
        long unavailableRuns = SqliteInspector.Scalar(
            app.DatabasePath,
            "SELECT COUNT(*) FROM PracticeLearningAttempts WHERE Status = 'Unavailable' AND TotalTests = 0");
        long sqlObservations = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM SqlLearningAttempts");
        long examAttempts = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM ExamAttempts");
        long declaredAttempts = SqliteInspector.Scalar(app.DatabasePath, "SELECT COUNT(*) FROM PracticeAttempts");
        Assert.True(provenRuns == 0, $"Exécutions prouvées attendues 0, trouvées {provenRuns}.");
        Assert.True(unavailableRuns == 1, $"Traces « Unavailable » attendues 1, trouvées {unavailableRuns}.");
        Assert.True(sqlObservations == 0, $"Observations SQL attendues 0, trouvées {sqlObservations}.");
        Assert.True(examAttempts == 0, $"Tentatives d'examen attendues 0, trouvées {examAttempts}.");
        Assert.True(declaredAttempts == 1, $"Tentatives déclarées attendues 1, trouvées {declaredAttempts}.");
        registry.Note("État persistant",
            "L'exécution demandée en mode manuel est tracée Status=Unavailable avec zéro test — jamais comme "
            + "réussite ; SqlLearningAttempts=0, ExamAttempts=0 ; une seule tentative déclarée.");

        registry.Conclude(
            "Exécuté intégralement. Navigation et apprentissage consultatif utilisables, indisponibilités "
            + "expliquées, export public seulement, aucune réussite automatique ni maîtrise créée par le mode manuel.");
    }
}
