using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeApiLab.Tests;

public sealed class ApiContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ApiContractTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task HealthIsAnonymousAndHealthy()
    {
        using HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MissingProofIsUnauthorizedAndReaderCannotCreate()
    {
        using HttpClient anonymous = _factory.CreateClient();
        using HttpResponseMessage unauthorized = await anonymous.PostAsJsonAsync("/orders", new { customer = "Ada", quantity = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using HttpClient reader = _factory.CreateAuthenticatedClient("forge-fake-reader-key");
        using HttpResponseMessage forbidden = await reader.PostAsJsonAsync("/orders", new { customer = "Ada", quantity = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task InvalidDtoReturnsProblemDetailsAndOperatorCreates()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient("forge-fake-operator-key");
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/orders", new { customer = "", quantity = 0 });
        string invalidBody = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest, $"Statut {(int)invalid.StatusCode}. Corps: {invalidBody}");
        Assert.Contains("application/problem+json", invalid.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);

        using HttpResponseMessage created = await client.PostAsJsonAsync("/orders", new { customer = " Grace ", quantity = 3 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.StartsWith("/orders/", created.Headers.Location?.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaginationIsBoundedAndMissingResourceIsNotFound()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient("forge-fake-reader-key");
        using HttpResponseMessage invalidPage = await client.GetAsync("/orders?page=1&pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        using HttpResponseMessage missing = await client.GetAsync("/orders/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
            services.AddDataProtection().UseEphemeralDataProtectionProvider());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Authentication:OperatorApiKey"] = "forge-fake-operator-key",
                ["Authentication:ReaderApiKey"] = "forge-fake-reader-key",
            }));
    }

    public HttpClient CreateAuthenticatedClient(string key)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Api-Key", key);
        return client;
    }
}
