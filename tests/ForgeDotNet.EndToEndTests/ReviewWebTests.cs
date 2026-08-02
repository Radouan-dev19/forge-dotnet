using System.Net;

namespace ForgeDotNet.EndToEndTests;

[Trait("Category", "ReviewScheduling")]
public sealed class ReviewWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    [Fact]
    public async Task EmptyQueueExplainsIntervalsAbsenceAndScoreIntegrity()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/reviews"));

        Assert.Contains("Calendrier transparent", html, StringComparison.Ordinal);
        Assert.Contains("J+1, J+3, J+7, J+14 puis J+30", html, StringComparison.Ordinal);
        Assert.Contains("ni pénalité d’absence", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ne modifient pas la maîtrise", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ajouter une carte personnelle", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Démarrer l’examen", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Rapport après clôture", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tableau de bord complet", html, StringComparison.OrdinalIgnoreCase);
    }
}
