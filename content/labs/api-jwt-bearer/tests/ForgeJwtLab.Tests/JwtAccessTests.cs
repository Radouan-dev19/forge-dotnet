using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ForgeJwtLab.Tests;

/// <summary>
/// Éprouve les quatre situations qui structurent l'authentification par jeton : anonyme,
/// périmé, insuffisant, valide — et la distinction 401/403 qui les sépare.
/// </summary>
public sealed class JwtAccessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JwtAccessTests(WebApplicationFactory<Program> factory)
    {
        // La configuration de test fournit les mêmes valeurs factices que appsettings.json :
        // la suite ne dépend pas du répertoire de travail au moment du lancement.
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestTokenFactory.Issuer,
                ["Jwt:Audience"] = TestTokenFactory.Audience,
                ["Jwt:SigningKey"] = TestTokenFactory.SigningKey,
            })));
    }

    [Fact]
    public async Task WithoutTokenTheProtectedRouteAnswers401()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WithAnExpiredTokenTheProtectedRouteAnswers401()
    {
        // Expiré depuis deux minutes : au-delà de la tolérance d'horloge de trente secondes.
        using HttpClient client = CreateClientWithToken(
            TestTokenFactory.CreateToken("orders.read", lifetimeSeconds: -120));

        using HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WithAWrongSigningKeyTheProtectedRouteAnswers401()
    {
        using HttpClient client = CreateClientWithToken(
            TestTokenFactory.CreateToken("orders.read", signingKey: "forge-fake-wrong-key-for-negative-test-0002"));

        using HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WithAnotherAudienceTheProtectedRouteAnswers401()
    {
        using HttpClient client = CreateClientWithToken(
            TestTokenFactory.CreateToken("orders.read", audience: "forge-reporting"));

        using HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WithAnInsufficientScopeTheWriteRouteAnswers403()
    {
        // Identité prouvée, droit absent : c'est un refus d'autorisation, pas d'authentification.
        using HttpClient client = CreateClientWithToken(TestTokenFactory.CreateToken("orders.read"));

        using HttpResponseMessage response = await client.PostAsync("/orders", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WithAValidTokenTheReadRouteAnswers200()
    {
        using HttpClient client = CreateClientWithToken(TestTokenFactory.CreateToken("orders.read"));

        using HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("order-1001", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WithTheWriteScopeTheWriteRouteAnswers201()
    {
        using HttpClient client = CreateClientWithToken(
            TestTokenFactory.CreateToken("orders.read orders.write"));

        using HttpResponseMessage response = await client.PostAsync("/orders", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TheHealthProbeStaysAnonymous()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClientWithToken(string token)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
