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

    /// <summary>
    /// En mode manuel, la page dit que l'installation ne peut rien valider, et comment y remédier.
    /// </summary>
    /// <remarks>
    /// La page affichait « porte A — fermée » sans distinguer deux situations qui n'appellent pas le
    /// même geste : un apprenant qui n'a pas encore travaillé, et une installation incapable de
    /// produire la moindre preuve parce qu'elle n'exécute aucun code. La seconde ne se répare pas en
    /// travaillant davantage, et c'est le mode livré par défaut — donc celui que voit un lecteur qui a
    /// suivi le README sans aller plus loin. Cette fabrique n'ayant pas de runner configuré, elle
    /// s'exécute précisément dans ce mode : le test est une lecture de ce que voit ce lecteur-là.
    /// </remarks>
    [Fact]
    public async Task InManualModeThePageSaysTheInstallationCannotProduceAnyProof()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/mastery"));

        Assert.Contains("Cette installation ne peut produire aucune preuve", html, StringComparison.Ordinal);
        Assert.Contains("mesurent une installation, pas votre niveau", html, StringComparison.Ordinal);
        // Un constat sans issue laisse le lecteur devant une porte fermée sans poignée : le message
        // doit nommer le script qui rend l'installation validante.
        Assert.Contains("scripts/build-code-runner.ps1", html, StringComparison.Ordinal);
    }
}
