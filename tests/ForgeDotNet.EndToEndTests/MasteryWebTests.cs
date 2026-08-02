using System.Net;

namespace ForgeDotNet.EndToEndTests;

public sealed class MasteryWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    [Fact]
    public async Task ReadOnlyProjectionShowsMissingEvidenceAndClosedGatesHonestly()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/mastery"));

        Assert.Contains("Projection explicable", html, StringComparison.Ordinal);
        Assert.Contains("absente — 0", html, StringComparison.Ordinal);
        Assert.Contains("A — Junior fiable — fermée", html, StringComparison.Ordinal);
        Assert.Contains("Aucun examen final vérifié et sans aide", html, StringComparison.Ordinal);
        Assert.DoesNotContain("prêt pour l’emploi", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Modifier le score", html, StringComparison.OrdinalIgnoreCase);
    }
}
