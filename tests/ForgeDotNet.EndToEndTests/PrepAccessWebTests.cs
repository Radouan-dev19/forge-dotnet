using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// Les dossiers d'entretiens ciblés sont personnels sur une application publiable : sans
/// déverrouillage, ni la liste ni un dossier ne laissent fuir un titre, un résumé ou un lien.
/// </summary>
/// <remarks>
/// La factory commune lève la garde pour les autres tests ; celle-ci la rétablit avec sa
/// configuration par défaut (mot de passe exigé, fichier secret absent) — le cas « échec fermé ».
/// </remarks>
public sealed class PrepAccessWebTests(PrepAccessWebTests.LockedPrepFactory factory)
    : IClassFixture<PrepAccessWebTests.LockedPrepFactory>
{
    [Theory]
    [InlineData("/prep")]
    [InlineData("/prep/icube-plan-veille-001")]
    [InlineData("/prep/icube-scripts-entretien-001")]
    public async Task ALockedSessionSeesTheGateAndNothingOfTheDossiers(string route)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(route);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Accès protégé", html, StringComparison.Ordinal);
        // Fichier secret absent : échec fermé, avec la procédure — jamais de formulaire trompeur.
        Assert.Contains("Aucun mot de passe n'est configuré", html, StringComparison.Ordinal);
        Assert.Contains(".secrets/prep-password.txt", html, StringComparison.Ordinal);
        // Rien du contenu personnel ne fuit : ni identifiant, ni titre, ni script.
        Assert.DoesNotContain("icube-plan-veille-001\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Plan de la veille", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Zone sensible", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ICube —", html, StringComparison.Ordinal);
    }

    public sealed class LockedPrepFactory : ForgeWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            // Rétablit le comportement livré : mot de passe exigé, secret pointé vers un fichier
            // inexistant du dossier de données jetable du test.
            builder.UseSetting("Prep:RequirePassword", "true");
            builder.UseSetting("Prep:PasswordFile", Path.Combine(DataDirectory, "prep-password-absent.txt"));
        }
    }
}
