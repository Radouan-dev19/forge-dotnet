using System.Net;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// Un identifiant d'activité cité dans une leçon mène directement à la page qui la sert.
/// </summary>
/// <remarks>
/// Le défaut fermé ici : la leçon S10 sur les index disait « ouvrez le scénario
/// <c>sql-covering-read-001</c> dans <c>/sql-lab</c> », mais la page SqlLab ne connaissait aucun
/// scénario par l'URL et l'apprenant devait le retrouver dans une liste déroulante. Le lien direct
/// s'ouvre dans un nouvel onglet pour revenir finir la leçon.
/// </remarks>
public sealed class LessonActivityLinkWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    [Fact]
    public async Task ASqlScenarioCitedInALessonLinksToTheSqlLabWithThatScenarioPreselected()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/learn/sql-index-plans-001"));

        Assert.Contains(
            "href=\"/sql-lab?scenario=sql-covering-read-001\" target=\"_blank\"",
            html,
            StringComparison.Ordinal);
        Assert.Contains("href=\"/sql-lab?scenario=sql-composite-access-001\"", html, StringComparison.Ordinal);
        Assert.Contains("nouvel onglet", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExerciseCitedInALessonLinksToItsPracticePageInANewTab()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/learn/api-cors-001"));

        Assert.Contains(
            "href=\"/practice/api-cors-origin-001\" target=\"_blank\"",
            html,
            StringComparison.Ordinal);
        // Un prérequis est une autre leçon : navigation du parcours, même onglet.
        Assert.Contains("href=\"/learn/api-http-semantics-001\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/learn/api-http-semantics-001\" target=\"_blank\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrdinaryInlineCodeStaysCodeAndTheSqlLabAcceptsAScenarioQuery()
    {
        using HttpClient client = factory.CreateClient();

        string lesson = WebUtility.HtmlDecode(await client.GetStringAsync("/learn/reference-types-001"));
        Assert.DoesNotContain("href=\"/practice/decimal\"", lesson, StringComparison.Ordinal);

        using HttpResponseMessage response = await client.GetAsync("/sql-lab?scenario=sql-covering-read-001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
