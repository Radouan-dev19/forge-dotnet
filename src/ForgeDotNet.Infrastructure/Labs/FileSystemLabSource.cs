using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Labs;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.Labs;

namespace ForgeDotNet.Infrastructure.Labs;

/// <summary>
/// Charge les laboratoires publiés depuis leur catalogue validé, brief compris.
/// </summary>
/// <remarks>
/// Le chargeur ne lit que le manifeste et le brief. Il ne touche ni au fichier de projet, ni à la
/// recette d'image, ni à la définition d'infrastructure : ces fichiers sont là pour que l'apprenant les
/// ouvre et les exécute, pas pour que le serveur les interprète. Publier leur contenu dans une vue
/// reviendrait à transporter du code exécutable dans l'interface sans nécessité.
/// </remarks>
public sealed class FileSystemLabSource(
    LabCatalog catalog,
    LabContentOptions options) : ILabSource, IDisposable
{
    private const int MaximumFileBytes = 131_072;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private Snapshot? _cache;

    private ContentCatalogProvider CatalogProvider => catalog.Provider;

    public async ValueTask<IReadOnlyList<Lab>> ListAsync(CancellationToken cancellationToken = default) =>
        (await GetSnapshotAsync(cancellationToken)).Labs;

    public async ValueTask<Lab?> GetAsync(string labId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(labId))
        {
            return null;
        }

        Snapshot snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.ById.GetValueOrDefault(labId);
    }

    public void Dispose() => _reloadGate.Dispose();

    private async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        string revision = CatalogProvider.Current.Revision;
        Snapshot? current = Volatile.Read(ref _cache);
        if (current is not null && string.Equals(current.Revision, revision, StringComparison.Ordinal))
        {
            return current;
        }

        await _reloadGate.WaitAsync(cancellationToken);
        try
        {
            revision = CatalogProvider.Current.Revision;
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
        var labs = new List<Lab>();
        foreach (ContentCatalogItem item in CatalogProvider.Current.GetByType(ContentDocumentType.Lab))
        {
            labs.Add(await LoadLabAsync(item, revision, cancellationToken));
        }

        Lab[] values = labs
            .OrderBy(item => item.Weeks[0])
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        return new Snapshot(
            revision,
            Array.AsReadOnly(values),
            values.ToDictionary(lab => lab.Id, StringComparer.Ordinal));
    }

    private async ValueTask<Lab> LoadLabAsync(
        ContentCatalogItem item,
        string revision,
        CancellationToken cancellationToken)
    {
        string labDirectory = ResolveDirectory(item.Id);
        string manifestText = await ReadFileAsync(Path.Combine(labDirectory, "lab.json"), cancellationToken);
        using JsonDocument manifest = JsonDocument.Parse(manifestText);
        JsonElement root = manifest.RootElement;
        if (!string.Equals(root.GetProperty("id").GetString(), item.Id, StringComparison.Ordinal)
            || root.GetProperty("version").GetInt32() != item.Version)
        {
            throw new InvalidDataException("Le manifeste de laboratoire ne correspond pas au catalogue publié.");
        }

        string briefRelativePath = root.GetProperty("briefPath").GetString()!;
        string briefPath = Path.GetFullPath(Path.Combine(labDirectory, briefRelativePath));
        EnsureDescendant(labDirectory, briefPath);
        string brief = await ReadFileAsync(briefPath, cancellationToken);

        return new Lab(
            item.Id,
            item.Version,
            root.GetProperty("title").GetString()!,
            Array.AsReadOnly(root.GetProperty("weeks").EnumerateArray().Select(week => week.GetInt32()).ToArray()),
            ReadStrings(root, "skills"),
            ReadStrings(root, "recommendedBefore"),
            root.GetProperty("estimatedMinutes").GetInt32(),
            brief,
            Array.AsReadOnly(root.GetProperty("objectives").EnumerateArray()
                .Select(objective => new LabObjective(
                    objective.GetProperty("id").GetString()!,
                    objective.GetProperty("goal").GetString()!,
                    objective.GetProperty("observableProof").GetString()!))
                .ToArray()),
            Array.AsReadOnly(root.GetProperty("commands").EnumerateArray()
                .Select(command => new LabCommand(
                    command.GetProperty("shell").GetString()!,
                    command.GetProperty("command").GetString()!,
                    command.GetProperty("purpose").GetString()!))
                .ToArray()),
            ReadStrings(root, "limits"),
            root.GetProperty("evidencePolicy").GetString()!,
            root.TryGetProperty("requiresDocker", out JsonElement docker) && docker.GetBoolean(),
            root.TryGetProperty("requiresNetwork", out JsonElement network) && network.GetBoolean(),
            revision);
    }

    private string ResolveDirectory(string labId)
    {
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string labRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.LabDirectoryPath));
        EnsureDescendant(contentRoot, labRoot);
        string labDirectory = Path.GetFullPath(Path.Combine(labRoot, labId));
        EnsureDescendant(labRoot, labDirectory);
        return labDirectory;
    }

    private static async ValueTask<string> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > MaximumFileBytes)
        {
            throw new InvalidDataException($"Fichier de laboratoire absent ou trop volumineux : {path}.");
        }

        return StrictUtf8.GetString(await File.ReadAllBytesAsync(path, cancellationToken));
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<string> ReadStrings(
        JsonElement element,
        string property) =>
        Array.AsReadOnly(element.GetProperty(property).EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray());

    private static void EnsureDescendant(string root, string candidate)
    {
        if (!candidate.StartsWith(
                Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Un chemin de laboratoire sort du catalogue publié.");
        }
    }

    private sealed record Snapshot(
        string Revision,
        IReadOnlyList<Lab> Labs,
        Dictionary<string, Lab> ById);
}
