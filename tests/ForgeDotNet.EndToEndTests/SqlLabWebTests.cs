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
}
