using System.Collections.Concurrent;
using System.Security.Cryptography;
using ForgeOAuthLab.Identity;

namespace ForgeOAuthLab.Client;

/// <summary>
/// Registre de state côté client : émis avant la redirection, consommés au retour.
/// </summary>
/// <remarks>
/// La consommation retire l'entrée en un geste : le premier retour gagne, tout rejeu de la
/// même adresse de rappel échoue ensuite — c'est la parade à la requête forgée inter-site.
/// </remarks>
public sealed class StateRegistry
{
    private readonly ConcurrentDictionary<string, byte> _pending = new(StringComparer.Ordinal);

    public string Issue()
    {
        string state = TokenCraft.ToBase64Url(RandomNumberGenerator.GetBytes(16));
        _pending[state] = 0;
        return state;
    }

    public bool TryConsume(string state) =>
        !string.IsNullOrWhiteSpace(state) && _pending.TryRemove(state, out _);
}
