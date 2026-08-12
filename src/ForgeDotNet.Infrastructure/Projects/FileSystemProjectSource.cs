using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Projects;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.Projects;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.Infrastructure.Projects;

/// <summary>
/// Charge les projets publiés depuis le catalogue, starter compris, sans jamais lire le corrigé de
/// référence : celui-ci n'existe que pour la vérification éditoriale hors ligne.
/// </summary>
public sealed class FileSystemProjectSource(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options) : IProjectSource, IDisposable
{
    private const int MaximumFileBytes = 131_072;
    private const int MaximumStarterFiles = 8;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private Snapshot? _cache;

    public async ValueTask<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default) =>
        (await GetSnapshotAsync(cancellationToken)).Projects;

    public async ValueTask<Project?> GetAsync(string projectId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return null;
        }

        Snapshot snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.ById.GetValueOrDefault(projectId);
    }

    public async ValueTask<(Project Project, ProjectAcceptanceSuite Suite)?> FindSuiteAsync(
        string runIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runIdentifier))
        {
            return null;
        }

        // Le découpage se fait sur le dernier point : « project-log-analyzer-001.synthese » désigne
        // le projet puis le jalon, et l'identifiant de projet en contient lui-même.
        int separator = runIdentifier.LastIndexOf('.');
        if (separator <= 0 || separator == runIdentifier.Length - 1)
        {
            return null;
        }

        Project? project = await GetAsync(runIdentifier[..separator], cancellationToken);
        ProjectAcceptanceSuite? suite = project?.AcceptanceSuites.FirstOrDefault(item =>
            string.Equals(item.MilestoneId, runIdentifier[(separator + 1)..], StringComparison.Ordinal));
        return project is null || suite is null ? null : (project, suite);
    }

    public void Dispose() => _reloadGate.Dispose();

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
        var projects = new List<Project>();
        foreach (ContentCatalogItem item in catalogProvider.Current.GetByType(ContentDocumentType.Project))
        {
            projects.Add(await LoadProjectAsync(item, revision, cancellationToken));
        }

        Project[] values = projects.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
        return new Snapshot(
            revision,
            Array.AsReadOnly(values),
            values.ToDictionary(project => project.Id, StringComparer.Ordinal));
    }

    private async ValueTask<Project> LoadProjectAsync(
        ContentCatalogItem item,
        string revision,
        CancellationToken cancellationToken)
    {
        string projectDirectory = ResolveDirectory(item.Id);
        string manifestText = await ReadFileAsync(Path.Combine(projectDirectory, "project.json"), cancellationToken);
        using JsonDocument manifest = JsonDocument.Parse(manifestText);
        JsonElement root = manifest.RootElement;
        if (!string.Equals(root.GetProperty("id").GetString(), item.Id, StringComparison.Ordinal)
            || root.GetProperty("version").GetInt32() != item.Version)
        {
            throw new InvalidDataException("Le manifeste de projet ne correspond pas au catalogue publié.");
        }

        string brief = await ReadFileAsync(
            Path.Combine(projectDirectory, root.GetProperty("briefPath").GetString()!),
            cancellationToken);

        var starter = new List<ProjectStarterFile>();
        if (root.TryGetProperty("starterPath", out JsonElement starterPath))
        {
            string starterDirectory = Path.GetFullPath(
                Path.Combine(projectDirectory, starterPath.GetString()!));
            EnsureDescendant(projectDirectory, starterDirectory);
            foreach (string file in Directory.EnumerateFiles(starterDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Take(MaximumStarterFiles))
            {
                starter.Add(new ProjectStarterFile(
                    Path.GetFileName(file),
                    await ReadFileAsync(file, cancellationToken)));
            }
        }

        var suites = new List<ProjectAcceptanceSuite>();
        if (root.TryGetProperty("acceptanceSuites", out JsonElement declaredSuites))
        {
            foreach (JsonElement suite in declaredSuites.EnumerateArray())
            {
                string suitePath = suite.GetProperty("suitePath").GetString()!;
                EnsureDescendant(projectDirectory, Path.GetFullPath(Path.Combine(projectDirectory, suitePath)));
                suites.Add(new ProjectAcceptanceSuite(
                    suite.GetProperty("milestoneId").GetString()!,
                    suitePath));
            }
        }

        return new Project(
            item.Id,
            item.Version,
            root.GetProperty("title").GetString()!,
            root.GetProperty("difficulty").GetInt32(),
            Array.AsReadOnly(root.GetProperty("weeks").EnumerateArray().Select(week => week.GetInt32()).ToArray()),
            ReadStrings(root, "skills"),
            root.GetProperty("estimatedHours").GetInt32(),
            brief,
            Array.AsReadOnly(starter.ToArray()),
            root.TryGetProperty("maximumSourceFiles", out JsonElement maximum) ? maximum.GetInt32() : 1,
            Array.AsReadOnly(root.GetProperty("milestones").EnumerateArray()
                .Select(milestone => new ProjectMilestone(
                    milestone.GetProperty("id").GetString()!,
                    milestone.GetProperty("title").GetString()!,
                    milestone.GetProperty("evidence").GetString()!,
                    ReadStrings(milestone, "acceptanceCriteria")))
                .ToArray()),
            Array.AsReadOnly(root.GetProperty("rubric").EnumerateArray()
                .Select(criterion => new ProjectRubricCriterion(
                    criterion.GetProperty("criterion").GetString()!,
                    criterion.GetProperty("weight").GetDecimal(),
                    criterion.GetProperty("observableEvidence").GetString()!))
                .ToArray()),
            Array.AsReadOnly(suites.ToArray()),
            ReadStrings(root, "commonMistakes"),
            root.TryGetProperty("achievementKey", out JsonElement achievementKey)
                ? achievementKey.GetString()
                : null,
            revision);
    }

    private string ResolveDirectory(string projectId)
    {
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string catalogDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.CatalogDirectoryPath));
        EnsureDescendant(contentRoot, catalogDirectory);
        string projectDirectory = Path.GetFullPath(Path.Combine(catalogDirectory, "projects", projectId));
        EnsureDescendant(catalogDirectory, projectDirectory);
        return projectDirectory;
    }

    private static async ValueTask<string> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > MaximumFileBytes)
        {
            throw new InvalidDataException($"Fichier de projet absent ou trop volumineux : {path}.");
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
            throw new InvalidOperationException("Un chemin de projet sort du catalogue publié.");
        }
    }

    private sealed record Snapshot(
        string Revision,
        IReadOnlyList<Project> Projects,
        Dictionary<string, Project> ById);
}
