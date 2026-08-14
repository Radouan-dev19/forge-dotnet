namespace ForgeDotNet.Infrastructure.Content;

public sealed class ContentValidationOptions
{
    // La banque de cartes de révision est volontairement un fichier unique — les tests de
    // chargement imposent un seul JSON sous reviews/ — et elle grandit avec chaque exercice
    // câblé : 188 exercices couverts dépassent 256 Kio. Cette borne est une garde contre un
    // fichier aberrant, pas un cliquet éditorial : la relever n'assouplit aucune règle de fond.
    public const long DefaultMaximumFileSizeBytes = 512 * 1024;
    public const int DefaultMaximumFiles = 10_000;
    public const int DefaultMaximumCloneOccurrences = 3;
    public const int DefaultMinimumCloneParagraphWords = 12;
    public const string DefaultLegacyDebtFileName = "authoring/content-debt.json";

    public required string ContentRootPath { get; init; }

    public string? SchemaRootPath { get; init; }

    public long MaximumFileSizeBytes { get; init; } = DefaultMaximumFileSizeBytes;

    public int MaximumFiles { get; init; } = DefaultMaximumFiles;

    /// <summary>
    /// Nombre maximal de documents distincts pouvant partager un même paragraphe normalisé.
    /// Au-delà, le lot est refusé : un contenu recopié n'enseigne pas la notion annoncée.
    /// </summary>
    public int MaximumCloneOccurrences { get; init; } = DefaultMaximumCloneOccurrences;

    /// <summary>
    /// Longueur minimale, en mots, d'un paragraphe soumis à la détection de recopie.
    /// Les titres et phrases courtes se répètent légitimement.
    /// </summary>
    public int MinimumCloneParagraphWords { get; init; } = DefaultMinimumCloneParagraphWords;

    /// <summary>
    /// Chemin du registre de dette éditoriale héritée, relatif à <see cref="ContentRootPath"/>
    /// ou absolu. Les défauts d'authenticité qui y sont déclarés sont tolérés ; ceux qui n'y
    /// sont pas déclarés refusent le lot, et une déclaration devenue inutile le refuse aussi.
    /// Une valeur nulle désactive toute tolérance.
    /// </summary>
    public string? LegacyDebtPath { get; init; } = DefaultLegacyDebtFileName;
}
