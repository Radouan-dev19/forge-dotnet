using System.Security.Cryptography;
using System.Text;

namespace ForgeDotNet.Web;

/// <summary>
/// Options d'accès à l'onglet « Entretiens ciblés » : les dossiers de préparation sont personnels
/// et l'application peut être publiée — l'onglet est donc verrouillé par mot de passe.
/// </summary>
/// <remarks>
/// Le mot de passe suit la même discipline que le secret du SqlLab : un fichier local sous
/// <c>.secrets/</c>, ignoré par Git, jamais dans la configuration versionnée ni dans les vues.
/// Fichier absent ou vide = onglet verrouillé sans issue (échec fermé), avec la procédure affichée.
/// <see cref="RequirePassword"/> n'existe que pour les tests Web, qui ne peuvent pas dérouler un
/// formulaire interactif : la valeur par défaut reste vraie partout ailleurs.
/// </remarks>
public sealed class PrepAccessOptions
{
    public required string PasswordFile { get; init; }

    /// <summary>
    /// Mot de passe fourni par la configuration (variable d'environnement <c>Prep__Password</c>),
    /// pour les hébergements où aucun fichier local ne peut être déposé — un conteneur reconstruit
    /// à chaque déploiement, par exemple. Le fichier local reste prioritaire quand il existe.
    /// </summary>
    public string? Password { get; init; }

    public bool RequirePassword { get; init; } = true;
}

/// <summary>
/// État d'accès aux dossiers de préparation, porté par le circuit Blazor : déverrouillé pour la
/// session de navigation en cours, verrouillé à nouveau au prochain chargement complet.
/// </summary>
public sealed class PrepAccessState(PrepAccessOptions options)
{
    private const int MaximumAttempts = 10;
    private int _attempts;

    public bool IsUnlocked { get; private set; }

    public bool PasswordConfigured => ReadConfiguredPassword() is not null;

    public string PasswordFilePath => options.PasswordFile;

    /// <summary>
    /// Vérifie le mot de passe en temps constant sur des empreintes SHA-256, avec un délai fixe et
    /// un plafond de tentatives par session : la page ne devient jamais un oracle de force brute.
    /// </summary>
    public async Task<bool> TryUnlockAsync(string? password, CancellationToken cancellationToken = default)
    {
        if (!options.RequirePassword)
        {
            IsUnlocked = true;
            return true;
        }

        if (IsUnlocked)
        {
            return true;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
        if (_attempts >= MaximumAttempts)
        {
            return false;
        }

        _attempts++;
        string? configured = ReadConfiguredPassword();
        if (configured is null || string.IsNullOrEmpty(password))
        {
            return false;
        }

        byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        byte[] provided = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        if (!CryptographicOperations.FixedTimeEquals(expected, provided))
        {
            return false;
        }

        IsUnlocked = true;
        return true;
    }

    public void ApplyDefault()
    {
        if (!options.RequirePassword)
        {
            IsUnlocked = true;
        }
    }

    private string? ReadConfiguredPassword()
    {
        try
        {
            if (File.Exists(options.PasswordFile))
            {
                string content = File.ReadAllText(options.PasswordFile).Trim();
                if (content.Length > 0)
                {
                    return content;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Illisible = verrouillé : l'accès échoue fermé, jamais ouvert.
            return null;
        }

        // Repli d'hébergement : la variable d'environnement Prep__Password, pour les conteneurs
        // reconstruits à chaque déploiement où aucun fichier local ne survit.
        string? fromConfiguration = options.Password?.Trim();
        return string.IsNullOrEmpty(fromConfiguration) ? null : fromConfiguration;
    }
}
