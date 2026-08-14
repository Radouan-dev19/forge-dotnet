using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using ForgeOAuthLab.Client;
using ForgeOAuthLab.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ForgeOAuthLab.Tests;

/// <summary>
/// Éprouve les deux flux du guichet local de bout en bout, et les trois refus qui font la
/// sécurité du dispositif : le mauvais code_verifier, le state rejoué, et le jeton d'accès
/// présenté comme jeton d'identité.
/// </summary>
public sealed class OAuthFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ClientId = "orders-web";
    private const string RedirectUri = "http://client.forge.local/callback";
    private const string Verifier = "forge-fake-test-verifier-abcdefghijklmnopqrs";

    private readonly WebApplicationFactory<Program> _factory;

    public OAuthFlowTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PkceFlowDeliversLinkedIdentityAndAccess()
    {
        using HttpClient client = CreateClient();
        var registry = new StateRegistry();
        string state = registry.Issue();
        string nonce = "n-flow-1";

        (string code, string returnedState) = await AuthorizeAsync(client, Verifier, state, nonce);

        Assert.True(registry.TryConsume(returnedState), "Le state du retour doit être celui émis.");

        using JsonDocument tokens = await ExchangeCodeAsync(client, code, Verifier);
        string accessToken = tokens.RootElement.GetProperty("access_token").GetString()!;
        string idToken = tokens.RootElement.GetProperty("id_token").GetString()!;

        Assert.Equal("accepted", IdTokenInspector.Inspect(idToken, nonce, ClientId, accessToken));
    }

    [Fact]
    public async Task WrongVerifierIsRefusedAtExchange()
    {
        using HttpClient client = CreateClient();
        (string code, _) = await AuthorizeAsync(client, Verifier, "st-wrong-1", "n-wrong-1");

        using HttpResponseMessage response = await PostTokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = "forge-fake-test-verifier-QUelconqueAutreSecret",
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_grant", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AuthorizationCodeIsSingleUse()
    {
        using HttpClient client = CreateClient();
        (string code, _) = await AuthorizeAsync(client, Verifier, "st-single-1", "n-single-1");

        using JsonDocument first = await ExchangeCodeAsync(client, code, Verifier);
        Assert.True(first.RootElement.TryGetProperty("access_token", out _));

        using HttpResponseMessage replay = await PostTokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = Verifier,
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
        });

        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public void ReplayedStateIsRefusedByTheClientRegistry()
    {
        var registry = new StateRegistry();
        string state = registry.Issue();

        Assert.True(registry.TryConsume(state), "Le premier retour consomme le state.");
        Assert.False(registry.TryConsume(state), "Le retour rejoué doit être refusé.");
    }

    [Fact]
    public async Task AccessTokenPresentedAsIdTokenIsRefused()
    {
        using HttpClient client = CreateClient();
        string nonce = "n-swap-1";
        (string code, _) = await AuthorizeAsync(client, Verifier, "st-swap-1", nonce);

        using JsonDocument tokens = await ExchangeCodeAsync(client, code, Verifier);
        string accessToken = tokens.RootElement.GetProperty("access_token").GetString()!;

        // L'audience d'un jeton d'accès est l'API, pas le client : l'inspection échoue
        // avant même de chercher un nonce.
        string verdict = IdTokenInspector.Inspect(accessToken, nonce, ClientId, accessToken);

        Assert.Equal("wrong-audience", verdict);
    }

    [Fact]
    public async Task ClientCredentialsIssuesAccessWithoutIdentity()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await PostTokenAsync(client, new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "billing-service",
            ["client_secret"] = "forge-fake-client-secret-0001",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument tokens = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(tokens.RootElement.TryGetProperty("access_token", out _));
        Assert.False(
            tokens.RootElement.TryGetProperty("id_token", out _),
            "Sans utilisateur, aucun jeton d'identité ne doit être émis.");
    }

    [Fact]
    public async Task WrongClientSecretIsRefused()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await PostTokenAsync(client, new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "billing-service",
            ["client_secret"] = "forge-fake-client-secret-9999",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<(string Code, string State)> AuthorizeAsync(
        HttpClient client,
        string verifier,
        string state,
        string nonce)
    {
        string challenge = TokenCraft.ToBase64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        string url = "/authorize?response_type=code"
            + $"&client_id={ClientId}"
            + $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}"
            + $"&code_challenge={challenge}&code_challenge_method=S256"
            + $"&state={Uri.EscapeDataString(state)}&nonce={Uri.EscapeDataString(nonce)}";

        using HttpResponseMessage response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var query = HttpUtility.ParseQueryString(response.Headers.Location!.Query);
        return (query["code"]!, query["state"]!);
    }

    private static async Task<JsonDocument> ExchangeCodeAsync(HttpClient client, string code, string verifier)
    {
        using HttpResponseMessage response = await PostTokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> PostTokenAsync(
        HttpClient client,
        Dictionary<string, string> form) =>
        client.PostAsync("/token", new FormUrlEncodedContent(form));
}
