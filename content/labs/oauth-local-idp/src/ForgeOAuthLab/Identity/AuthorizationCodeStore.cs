using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ForgeOAuthLab.Identity;

/// <summary>Ce que le guichet mémorise avec chaque code émis, en attendant l'échange.</summary>
public sealed record IssuedCode(string ClientId, string RedirectUri, string CodeChallenge, string Nonce);

/// <summary>
/// Registre des codes d'autorisation : émis à la redirection, consommés à l'échange.
/// </summary>
/// <remarks>
/// Le retrait atomique fait l'usage unique : la seconde présentation d'un code ne trouve
/// plus rien, ce qui est le comportement exigé — un code rejoué signale un vol.
/// </remarks>
public sealed class AuthorizationCodeStore
{
    private readonly ConcurrentDictionary<string, IssuedCode> _codes = new(StringComparer.Ordinal);

    public string Issue(IssuedCode issued)
    {
        string code = TokenCraft.ToBase64Url(RandomNumberGenerator.GetBytes(24));
        _codes[code] = issued;
        return code;
    }

    public bool TryConsume(string code, out IssuedCode issued) =>
        _codes.TryRemove(code, out issued!);
}
