using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ForgeApiLab.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationSchemeName = "ApiKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var supplied) || string.IsNullOrWhiteSpace(supplied))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? role = Matches(supplied!, ReadConfiguredSecret("Operator")) ? "Operator"
            : Matches(supplied!, ReadConfiguredSecret("Reader")) ? "Reader"
            : null;
        if (role is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Preuve d’authentification invalide."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, role.ToLowerInvariant()), new Claim(ClaimTypes.Role, role)],
            AuthenticationSchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationSchemeName)));
    }

    private string? ReadConfiguredSecret(string identity)
    {
        string? direct = configuration[$"Authentication:{identity}ApiKey"];
        if (!string.IsNullOrWhiteSpace(direct)) return direct;
        string? path = configuration[$"Authentication:{identity}ApiKeyFile"];
        if (string.IsNullOrWhiteSpace(path)) return null;
        var info = new FileInfo(Path.GetFullPath(path));
        if (!info.Exists || info.Length is < 8 or > 4096) return null;
        return File.ReadAllText(info.FullName).Trim();
    }

    private static bool Matches(string supplied, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return false;
        byte[] left = Encoding.UTF8.GetBytes(supplied);
        byte[] right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
