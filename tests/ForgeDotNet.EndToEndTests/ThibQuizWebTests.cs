using System.Net;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// L'onglet Thib sert ses banques et ses questions, annonce qu'il ne prouve rien, et ne rend
/// jamais la clé ni l'explication avant la vérification.
/// </summary>
public sealed class ThibQuizWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    [Fact]
    public async Task TheIndexListsEveryBankAndClaimsNoMasteryProof()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/thib"));

        foreach (string bankId in new[]
        {
            "thib-runtime-langage-001", "thib-async-concurrence-002",
            "thib-donnees-ef-sql-003", "thib-web-archi-securite-004",
        })
        {
            Assert.Contains($"/thib/{bankId}", html, StringComparison.Ordinal);
        }

        Assert.Contains("ne produit de preuve de maîtrise", html, StringComparison.Ordinal);
        Assert.Contains("hors parcours", html, StringComparison.Ordinal);
        Assert.Contains("une ou plusieurs réponses justes", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ABankPageRendersItsQuestionsWithFiveOptionsAndHidesTheKey()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/thib/thib-runtime-langage-001"));

        Assert.Contains("Runtime .NET et langage C#", html, StringComparison.Ordinal);
        Assert.Contains("Question 1 — Ramasse-miettes", html, StringComparison.Ordinal);
        Assert.Contains("Question 10 — Interfaces modernes", html, StringComparison.Ordinal);
        Assert.Contains("aucune preuve de maîtrise", html, StringComparison.Ordinal);
        // Cinq propositions par question, le nombre à trouver est annoncé.
        Assert.Contains("<strong>E.</strong>", html, StringComparison.Ordinal);
        Assert.Contains("3 réponses justes à cocher.", html, StringComparison.Ordinal);
        Assert.Contains("Vérifier ma réponse", html, StringComparison.Ordinal);
        // Ni la clé, ni l'explication, ni le marquage des options ne sont rendus avant vérification.
        Assert.DoesNotContain("Clé :", html, StringComparison.Ordinal);
        Assert.DoesNotContain("— juste", html, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.WaitForPendingFinalizers()", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnknownBankIsReportedNotFabricated()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/thib/thib-inconnue-999");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Banque de quiz introuvable.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Question 1", html, StringComparison.Ordinal);
    }
}
