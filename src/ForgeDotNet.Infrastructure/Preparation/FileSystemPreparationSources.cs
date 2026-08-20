using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Preparation;
using ForgeDotNet.Domain.Ai;
using ForgeDotNet.Domain.Career;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.English;
using ForgeDotNet.Domain.Interviews;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.Infrastructure.Preparation;

/// <summary>
/// Base commune aux deux sources de préparation : un manifeste JSON plat par document publié.
/// </summary>
/// <remarks>
/// Fiches d'entretien et cartes d'anglais vivent chacune dans un dossier plat du catalogue, un fichier
/// par document — contrairement aux exercices et aux projets, qui portent une arborescence. Le
/// chargement se réduit donc à lire un fichier nommé d'après l'identifiant publié, ce qui rend la
/// vérification de chemin d'autant plus nécessaire : l'identifiant vient du catalogue, mais il compose
/// un chemin, et rien ne doit pouvoir sortir du dossier de sa famille.
/// </remarks>
public abstract class FileSystemPreparationSource<T>(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options,
    ContentDocumentType documentType,
    string directoryName) : IDisposable
{
    private const int MaximumFileBytes = 65_536;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private Snapshot? _cache;

    protected abstract T Read(ContentCatalogItem item, JsonElement root);

    public async ValueTask<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        (await GetSnapshotAsync(cancellationToken)).Items;

    public async ValueTask<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return default;
        }

        Snapshot snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.ById.TryGetValue(id, out T? value) ? value : default;
    }

    public void Dispose()
    {
        _reloadGate.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract string IdentifierOf(T value);

    protected static ReadOnlyCollection<string> ReadStrings(JsonElement element, string property) =>
        Array.AsReadOnly(element.GetProperty(property).EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray());

    private async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        string revision = catalogProvider.Current.Revision;
        Snapshot? current = Volatile.Read(ref _cache);
        if (current is not null && string.Equals(current.Revision, revision, StringComparison.Ordinal))
        {
            return current;
        }

        await _reloadGate.WaitAsync(cancellationToken);
        try
        {
            revision = catalogProvider.Current.Revision;
            current = Volatile.Read(ref _cache);
            if (current is not null && string.Equals(current.Revision, revision, StringComparison.Ordinal))
            {
                return current;
            }

            Snapshot replacement = await LoadAsync(revision, cancellationToken);
            Volatile.Write(ref _cache, replacement);
            return replacement;
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private async ValueTask<Snapshot> LoadAsync(string revision, CancellationToken cancellationToken)
    {
        var values = new List<T>();
        foreach (ContentCatalogItem item in catalogProvider.Current.GetByType(documentType))
        {
            string path = ResolvePath(item.Id);
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > MaximumFileBytes)
            {
                throw new InvalidDataException($"Document de préparation absent ou trop volumineux : {path}.");
            }

            string text = StrictUtf8.GetString(await File.ReadAllBytesAsync(path, cancellationToken));
            using JsonDocument document = JsonDocument.Parse(text);
            JsonElement root = document.RootElement;
            if (!string.Equals(root.GetProperty("id").GetString(), item.Id, StringComparison.Ordinal)
                || root.GetProperty("version").GetInt32() != item.Version)
            {
                throw new InvalidDataException("Le manifeste de préparation ne correspond pas au catalogue publié.");
            }

            values.Add(Read(item, root));
        }

        T[] ordered = values.OrderBy(IdentifierOf, StringComparer.Ordinal).ToArray();
        return new Snapshot(
            revision,
            Array.AsReadOnly(ordered),
            ordered.ToDictionary(IdentifierOf, StringComparer.Ordinal));
    }

    private string ResolvePath(string id)
    {
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string catalogDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.CatalogDirectoryPath));
        EnsureDescendant(contentRoot, catalogDirectory);
        string familyDirectory = Path.GetFullPath(Path.Combine(catalogDirectory, directoryName));
        EnsureDescendant(catalogDirectory, familyDirectory);
        string path = Path.GetFullPath(Path.Combine(familyDirectory, $"{id}.json"));
        EnsureDescendant(familyDirectory, path);
        return path;
    }

    private static void EnsureDescendant(string root, string candidate)
    {
        if (!candidate.StartsWith(
                Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Un chemin de préparation sort du catalogue publié.");
        }
    }

    private sealed record Snapshot(string Revision, IReadOnlyList<T> Items, Dictionary<string, T> ById);
}

public sealed class FileSystemInterviewSource(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options)
    : FileSystemPreparationSource<InterviewSheet>(
        catalogProvider,
        options,
        ContentDocumentType.InterviewQuestion,
        "interviews"), IInterviewSource
{
    public async ValueTask<InterviewSheet?> FindForExerciseAsync(
        string exerciseId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exerciseId))
        {
            return null;
        }

        return await GetAsync($"interview-{exerciseId}", cancellationToken);
    }

    protected override string IdentifierOf(InterviewSheet value) => value.Id;

    protected override InterviewSheet Read(ContentCatalogItem item, JsonElement root) => new(
        item.Id,
        item.Version,
        root.GetProperty("title").GetString()!,
        InterviewLevels.Parse(root.GetProperty("level").GetString()),
        root.GetProperty("durationMinutes").GetInt32(),
        ReadStrings(root, "skills"),
        root.GetProperty("question").GetString()!,
        ReadStrings(root, "observableCriteria"),
        root.GetProperty("modelAnswer").GetString()!,
        ReadStrings(root, "commonMistakes"),
        ReadStrings(root, "variants"));
}

public sealed class FileSystemEnglishActivitySource(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options)
    : FileSystemPreparationSource<EnglishActivity>(
        catalogProvider,
        options,
        ContentDocumentType.EnglishActivity,
        "english"), IEnglishActivitySource
{
    protected override string IdentifierOf(EnglishActivity value) => value.Id;

    protected override EnglishActivity Read(ContentCatalogItem item, JsonElement root) => new(
        item.Id,
        item.Version,
        root.GetProperty("title").GetString()!,
        root.GetProperty("level").GetString()!,
        root.GetProperty("durationMinutes").GetInt32(),
        ReadStrings(root, "skills"),
        root.GetProperty("situation").GetString()!,
        ReadStrings(root, "instructions"),
        Array.AsReadOnly(root.GetProperty("vocabulary").EnumerateArray()
            .Select(term => new EnglishTerm(
                term.GetProperty("term").GetString()!,
                term.GetProperty("meaning").GetString()!))
            .ToArray()),
        ReadStrings(root, "expectedElements"),
        root.GetProperty("modelAnswer").GetString()!,
        ReadStrings(root, "commonMistakes"),
        ReadStrings(root, "variants"));
}

public sealed class FileSystemCareerGuideSource(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options)
    : FileSystemPreparationSource<CareerGuide>(
        catalogProvider,
        options,
        ContentDocumentType.CareerGuide,
        "career"), ICareerGuideSource
{
    private const int MaximumBodyBytes = 65_536;
    private static readonly UTF8Encoding StrictBodyUtf8 = new(false, true);

    private readonly string _careerDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
        Path.Combine(options.CatalogDirectoryPath, "career")));

    protected override string IdentifierOf(CareerGuide value) => value.Id;

    protected override CareerGuide Read(ContentCatalogItem item, JsonElement root) => new(
        item.Id,
        item.Version,
        root.GetProperty("title").GetString()!,
        root.GetProperty("summary").GetString()!,
        root.GetProperty("order").GetInt32(),
        ReadBody(root.GetProperty("documentPath").GetString()!));

    /// <summary>
    /// Lit le Markdown du guide, dont le nom vient d'un manifeste validé par schéma : un simple nom
    /// de fichier, sans séparateur. La garde de descendance reste appliquée par principe.
    /// </summary>
    private string ReadBody(string documentPath)
    {
        string resolved = Path.GetFullPath(Path.Combine(_careerDirectory, documentPath));
        if (!resolved.StartsWith(_careerDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Le document d'un guide de carrière sort de son dossier.");
        }

        var info = new FileInfo(resolved);
        if (!info.Exists || info.Length == 0 || info.Length > MaximumBodyBytes)
        {
            throw new InvalidDataException($"Document de guide de carrière absent, vide ou trop volumineux : {documentPath}.");
        }

        return StrictBodyUtf8.GetString(File.ReadAllBytes(resolved));
    }
}

public sealed class FileSystemAiGuideSource(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options)
    : FileSystemPreparationSource<AiGuide>(
        catalogProvider,
        options,
        ContentDocumentType.AiGuide,
        "ai"), IAiGuideSource
{
    private const int MaximumGuideBytes = 65_536;
    private static readonly UTF8Encoding StrictGuideUtf8 = new(false, true);

    private readonly string _aiDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
        Path.Combine(options.CatalogDirectoryPath, "ai")));

    protected override string IdentifierOf(AiGuide value) => value.Id;

    protected override AiGuide Read(ContentCatalogItem item, JsonElement root) => new(
        item.Id,
        item.Version,
        root.GetProperty("title").GetString()!,
        root.GetProperty("summary").GetString()!,
        root.GetProperty("order").GetInt32(),
        ReadBody(root.GetProperty("documentPath").GetString()!));

    /// <summary>
    /// Le nom du Markdown vient d'un manifeste validé par schéma ; la garde de descendance reste
    /// appliquée par principe, comme pour chaque famille plate du catalogue.
    /// </summary>
    private string ReadBody(string documentPath)
    {
        string resolved = Path.GetFullPath(Path.Combine(_aiDirectory, documentPath));
        if (!resolved.StartsWith(_aiDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Le document d'un guide IA sort de son dossier.");
        }

        var info = new FileInfo(resolved);
        if (!info.Exists || info.Length == 0 || info.Length > MaximumGuideBytes)
        {
            throw new InvalidDataException($"Document de guide IA absent, vide ou trop volumineux : {documentPath}.");
        }

        return StrictGuideUtf8.GetString(File.ReadAllBytes(resolved));
    }
}
