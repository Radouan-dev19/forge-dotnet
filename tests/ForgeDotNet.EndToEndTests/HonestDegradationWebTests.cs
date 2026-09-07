using System.Net;
using System.Text.RegularExpressions;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// Une installation qui ne peut rien prouver le dit partout où l'apprenant pourrait travailler pour rien,
/// et chaque document cité par une page est servi par l'application.
/// </summary>
/// <remarks>
/// L'audit avait relevé que <c>/mastery</c> portait la bonne bannière mais pas <c>/exams</c> ni
/// <c>/debug-lab</c> : un examen de deux heures pouvait être passé puis noté zéro sans avertissement. La
/// factory de test force le mode manuel, ce qui est précisément la situation à décrire honnêtement.
/// </remarks>
public sealed class HonestDegradationWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    [Theory]
    [InlineData("/practice")]
    [InlineData("/projects")]
    [InlineData("/debug-lab")]
    [InlineData("/exams")]
    [InlineData("/dashboard")]
    [InlineData("/reviews")]
    public async Task EveryPageThatCouldWasteEffortInManualModeNamesTheRemedy(string route)
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync(route));

        Assert.Contains("Cette installation ne peut produire aucune preuve.", html, StringComparison.Ordinal);
        Assert.Contains("scripts/build-code-runner.ps1", html, StringComparison.Ordinal);
        Assert.Contains("--CodeRunner:Mode Docker", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExamCannotBeStartedInManualModeAndTheSqlBankNamesItsExtraRequirement()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/exams"));

        Assert.Contains("Démarrage désactivé — mode manuel", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Démarrer l’examen<", html, StringComparison.Ordinal);
        Assert.Contains("exige en plus le SqlLab", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheDebugLabIndexCountsItsPublishedScenariosInsteadOfAStaleNumber()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/debug-lab"));

        Assert.DoesNotContain("Huit scénarios", html, StringComparison.Ordinal);
        Assert.Contains("30 scénario(s) publié(s)", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/docs/HUMAN_REVIEW.md", "Revue humaine")]
    [InlineData("/docs/HUMAN_PANEL_KIT.md", "panel")]
    [InlineData("/docs/README.md", "Valider des exercices")]
    public async Task DocumentsCitedByPagesAreServedReadOnly(string route, string expectedFragment)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(route);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedFragment, html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lecture seule", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADocumentOutsideTheAllowListIsRefusedWithoutTouchingTheDisk()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/docs/..%2Fappsettings.json"));

        Assert.Contains("n'est pas publié par l'application", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PagesLinkToTheServedDocumentsInsteadOfNamingFiles()
    {
        using HttpClient client = factory.CreateClient();

        string humanReview = await client.GetStringAsync("/human-review");
        string interviews = await client.GetStringAsync("/interviews");
        string career = await client.GetStringAsync("/career");

        Assert.Contains("href=\"/docs/HUMAN_REVIEW.md\"", humanReview, StringComparison.Ordinal);
        Assert.Contains("href=\"/docs/HUMAN_PANEL_KIT.md\"", humanReview, StringComparison.Ordinal);
        Assert.Contains("href=\"/docs/HUMAN_REVIEW.md\"", interviews, StringComparison.Ordinal);
        Assert.Contains("href=\"/career/export-script\"", career, StringComparison.Ordinal);

        using HttpResponseMessage script = await client.GetAsync("/career/export-script");
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        Assert.Contains("param", await script.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/career/career-cv-evidence-001", "<h3 id=\"title-")]
    [InlineData("/cloud/cloud-fondamentaux-001", "<h3 id=\"title-")]
    [InlineData("/ai/ai-modele-mental-001", "<h3 id=\"title-")]
    [InlineData("/learn/week-0/week0-iis-001", "<h3 id=\"title-")]
    // Le brief d'un laboratoire n'a pas de titres de niveau 2 : ce sont ses blocs de commandes qui
    // prouvent le rendu typé.
    [InlineData("/labs/api-mini-erp", "aria-label=\"Exemple de code\"")]
    public async Task GuidesAndBriefsAreRenderedAsMarkdownNotDumpedAsRawText(string route, string typedMarker)
    {
        using HttpClient client = factory.CreateClient();

        string html = await client.GetStringAsync(route);

        Assert.DoesNotContain("<pre class=\"runner-output\">#", html, StringComparison.Ordinal);
        Assert.Contains("markdown-document", html, StringComparison.Ordinal);
        Assert.Contains(typedMarker, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheAboutPageDescribesTheRealLimitsInsteadOfDenyingShippedFeatures()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/about"));

        Assert.DoesNotContain("ne sont pas encore disponibles", html, StringComparison.Ordinal);
        Assert.Contains("ne produit aucune preuve de maîtrise", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ASeniorLessonLinksItsExerciseAndOffersACheckableQuiz()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/learn-senior/senior-resilience-001"));

        Assert.Contains("href=\"/practice/senior-circuit-breaker-001\" target=\"_blank\"", html, StringComparison.Ordinal);
        Assert.Contains("Vérifier ma réponse", html, StringComparison.Ordinal);
        Assert.Contains("name=\"senior-lesson-quiz\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ALessonCitingALabByFilePathLinksToTheLabPage()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/learn/api-openapi-contracts-001"));

        Assert.Contains("href=\"/labs/api-mini-erp\" target=\"_blank\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un dossier de préparation relie les cours de la plateforme : ses liens internes doivent être
    /// rendus comme de vrais liens, pas comme du texte — c'est sa seule raison d'être.
    /// </summary>
    [Fact]
    public async Task APrepDossierRendersItsInternalCourseLinks()
    {
        using HttpClient client = factory.CreateClient();

        string plan = WebUtility.HtmlDecode(await client.GetStringAsync("/prep/icube-plan-veille-001"));
        Assert.Contains("href=\"/learn/week-0/week0-microservices-001\"", plan, StringComparison.Ordinal);
        Assert.Contains("href=\"/learn/week-0/week0-azure-service-bus-001\"", plan, StringComparison.Ordinal);
        Assert.Contains("href=\"/career/career-star-workbook-001\"", plan, StringComparison.Ordinal);
        Assert.Contains("href=\"/prep/icube-scripts-entretien-001\"", plan, StringComparison.Ordinal);
        Assert.Contains("href=\"/learn-senior/senior-boundaries-001\"", plan, StringComparison.Ordinal);

        string scripts = WebUtility.HtmlDecode(await client.GetStringAsync("/prep/icube-scripts-entretien-001"));
        Assert.Contains("Zone sensible", scripts, StringComparison.Ordinal);
        Assert.Contains("aucune preuve de maîtrise", scripts, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/prep/icube-plan-veille-001")]
    [InlineData("/prep/icube-scripts-entretien-001")]
    public async Task APrepDossierHasNavigableSectionsAndOnlyPublishedInternalResources(string route)
    {
        using HttpClient client = factory.CreateClient();
        string html = WebUtility.HtmlDecode(await client.GetStringAsync(route));

        Assert.Contains("Sommaire du dossier", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<pre class=\"runner-output\">#", html, StringComparison.Ordinal);

        MatchCollection anchors = Regex.Matches(html, $"href=\"{Regex.Escape(route)}#([^\"]+)\"");
        Assert.True(anchors.Count > 1, "Les liens du sommaire doivent conserver la route du dossier malgré la base HTML.");
        foreach (Match anchor in anchors)
        {
            Assert.Contains($"id=\"{anchor.Groups[1].Value}\"", html, StringComparison.Ordinal);
        }

        string[] resources = Regex.Matches(
                html,
                "href=\"(/(?:learn(?:-senior)?|practice|labs|career|prep)/[^\"#?]+)\"")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(resources);
        foreach (string resource in resources)
        {
            using HttpResponseMessage response = await client.GetAsync(resource);
            string body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode, $"Ressource inaccessible : {resource}");
            Assert.DoesNotContain("class=\"error-message\"", body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task GateRequirementLabelsReadAsRequirementsNotAsStates()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/mastery"));

        Assert.Contains("Exige : Porte A ouverte", html, StringComparison.Ordinal);
    }
}
