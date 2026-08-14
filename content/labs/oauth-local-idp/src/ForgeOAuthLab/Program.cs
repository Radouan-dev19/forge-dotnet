using System.Security.Cryptography;
using System.Text;
using ForgeOAuthLab.Identity;

var builder = WebApplication.CreateBuilder(args);

// Le registre des codes vit en mémoire du processus : c'est un guichet pédagogique,
// entièrement hors ligne, qui montre la mécanique au lieu de la configurer.
builder.Services.AddSingleton<AuthorizationCodeStore>();

var app = builder.Build();

// Deux clients enregistrés en dur : un public (PKCE) et un confidentiel (secret factice).
const string PublicClientId = "orders-web";
const string PublicRedirectUri = "http://client.forge.local/callback";
const string ConfidentialClientId = "billing-service";
const string ConfidentialClientSecret = "forge-fake-client-secret-0001";

app.MapGet("/health", () => Results.Ok(new { status = "ready" }));

// Point d'autorisation : authentifie un utilisateur factice, mémorise l'empreinte PKCE
// avec le code émis, et renvoie code + state par redirection — comme un vrai guichet.
app.MapGet("/authorize", (HttpRequest request, AuthorizationCodeStore codes) =>
{
    string responseType = request.Query["response_type"].ToString();
    string clientId = request.Query["client_id"].ToString();
    string redirectUri = request.Query["redirect_uri"].ToString();
    string challenge = request.Query["code_challenge"].ToString();
    string method = request.Query["code_challenge_method"].ToString();
    string state = request.Query["state"].ToString();
    string nonce = request.Query["nonce"].ToString();

    // L'adresse de rappel se compare exactement à celle enregistrée : pas de préfixe.
    if (responseType != "code"
        || clientId != PublicClientId
        || redirectUri != PublicRedirectUri
        || string.IsNullOrEmpty(challenge)
        || method != "S256"
        || string.IsNullOrEmpty(state)
        || string.IsNullOrEmpty(nonce))
    {
        return Results.BadRequest(new { error = "invalid_request" });
    }

    // Ici, un vrai guichet authentifierait l'utilisateur et recueillerait le consentement.
    string code = codes.Issue(new IssuedCode(clientId, redirectUri, challenge, nonce));

    return Results.Redirect(
        $"{redirectUri}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}");
});

// Point d'échange : les deux flux du laboratoire, et rien d'autre.
app.MapPost("/token", async (HttpRequest request, AuthorizationCodeStore codes) =>
{
    IFormCollection form = await request.ReadFormAsync();
    string grantType = form["grant_type"].ToString();
    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    if (grantType == "authorization_code")
    {
        string code = form["code"].ToString();
        string verifier = form["code_verifier"].ToString();
        string clientId = form["client_id"].ToString();
        string redirectUri = form["redirect_uri"].ToString();

        // Le retrait fait l'usage unique : un code rejoué ne trouve plus rien.
        if (!codes.TryConsume(code, out IssuedCode issued)
            || issued.ClientId != clientId
            || issued.RedirectUri != redirectUri
            || !PkceMatches(verifier, issued.CodeChallenge))
        {
            return Results.Json(new { error = "invalid_grant" }, statusCode: 400);
        }

        string accessToken = TokenCraft.IssueAccessToken("user-demo", "orders.read", now);
        string idToken = TokenCraft.IssueIdToken("user-demo", clientId, issued.Nonce, accessToken, now);

        return Results.Json(new
        {
            access_token = accessToken,
            id_token = idToken,
            token_type = "Bearer",
            expires_in = 300,
        });
    }

    if (grantType == "client_credentials")
    {
        string clientId = form["client_id"].ToString();
        string clientSecret = form["client_secret"].ToString();

        // Le secret du client confidentiel se compare en temps constant.
        bool authenticated = clientId == ConfidentialClientId
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(clientSecret),
                Encoding.UTF8.GetBytes(ConfidentialClientSecret));

        if (!authenticated)
        {
            return Results.Json(new { error = "invalid_client" }, statusCode: 400);
        }

        // Machine à machine : un jeton d'accès au nom du client, et AUCUN jeton
        // d'identité — il n'y a personne dont attester l'identité.
        string accessToken = TokenCraft.IssueAccessToken(clientId, "billing.run", now);

        return Results.Json(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 300,
        });
    }

    return Results.Json(new { error = "unsupported_grant_type" }, statusCode: 400);
});

app.Run();

static bool PkceMatches(string verifier, string storedChallenge)
{
    if (string.IsNullOrEmpty(verifier) || string.IsNullOrEmpty(storedChallenge))
    {
        return false;
    }

    byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
    string recomputed = TokenCraft.ToBase64Url(hash);

    return CryptographicOperations.FixedTimeEquals(
        Encoding.ASCII.GetBytes(recomputed),
        Encoding.ASCII.GetBytes(storedChallenge));
}

/// <summary>Point d'entrée exposé à la suite de tests, qui monte le guichet en mémoire.</summary>
public partial class Program;
