using System.Net;

namespace ForgeDotNet.EndToEndTests;

public sealed class SqlLabWebTests(ForgeWebApplicationFactory factory)
    : IClassFixture<ForgeWebApplicationFactory>
{
    [Fact]
    public async Task SqlLabPageIsHonestWhenDisabledAndExposesNoConnectionSecret()
    {
        using HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        HttpResponseMessage response = await client.GetAsync("/sql-lab");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Sans service SQL Server isolé", html, StringComparison.Ordinal);
        Assert.Contains("aucune validation SQL", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MSSQL_SA_PASSWORD", html, StringComparison.Ordinal);
        Assert.DoesNotContain("forge_user_", html, StringComparison.Ordinal);
        Assert.DoesNotContain("14333", html, StringComparison.Ordinal);

        HttpResponseMessage health = await client.GetAsync("/health/sql-lab");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    /// <summary>
    /// P1-03 : la page annonçait ne contenir aucun scénario pédagogique alors que quarante étaient
    /// livrés. Elle doit maintenant les proposer, sans jamais exposer leur résultat de référence.
    /// </summary>
    [Fact]
    public async Task PublishedScenariosAreOfferedWithoutLeakingReferenceResultsOrSolutions()
    {
        using HttpClient client = factory.CreateClient();

        string html = WebUtility.HtmlDecode(await client.GetStringAsync("/sql-lab"));

        Assert.Contains("35 scénario(s) publié(s) sont exécutables ici", html, StringComparison.Ordinal);
        Assert.DoesNotContain("aucun des douze", html, StringComparison.OrdinalIgnoreCase);

        // Un scénario réel est proposé au choix, avec son identité de contenu.
        Assert.Contains("sql-monthly-cte-001", html, StringComparison.Ordinal);
        Assert.Contains("Nommer une agrégation mensuelle", html, StringComparison.Ordinal);

        // Le résultat de référence et la requête équivalente restent strictement serveur.
        Assert.DoesNotContain("WITH Monthly", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expectedRows", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("equivalentQuery", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dirtySql", html, StringComparison.OrdinalIgnoreCase);
    }
}
