using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Curriculum;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Curriculum;

public sealed class FileSystemLessonContentSource : ILessonContentSource
{
    private const int MaximumFileBytes = 256 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly ContentCatalogProvider _catalogProvider;
    private readonly string _catalogDirectory;
    private readonly string _contentRoot;
    private readonly string _curriculumId;

    public FileSystemLessonContentSource(
        ContentCatalogProvider catalogProvider,
        LessonContentOptions options)
    {
        ArgumentNullException.ThrowIfNull(catalogProvider);
        ArgumentNullException.ThrowIfNull(options);
        _catalogProvider = catalogProvider;
        _contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        _catalogDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.CatalogDirectoryPath));
        _curriculumId = options.CurriculumId;
        EnsureContained(_catalogDirectory, _contentRoot);
    }

    public async ValueTask<LessonLibraryView> GetLibraryAsync(
        CancellationToken cancellationToken = default)
    {
        ContentCatalog catalog = _catalogProvider.Current;
        ContentCatalogItem curriculum = catalog.FindById(_curriculumId)
            ?? throw new InvalidDataException("Le parcours configuré n'existe pas dans le catalogue publié.");
        if (curriculum.Type != ContentDocumentType.Curriculum)
        {
            throw new InvalidDataException("Le parcours configuré ne cible pas un manifeste de parcours.");
        }

        string manifestPath = ResolveFile(
            $"curriculum/{_curriculumId}.json",
            _catalogDirectory);
        string json = await ReadTextAsync(manifestPath, cancellationToken);
        using JsonDocument document = JsonDocument.Parse(json, JsonOptions);
        JsonElement root = document.RootElement;
        var modules = new List<CurriculumModuleView>();
        foreach (JsonElement module in root.GetProperty("modules").EnumerateArray())
        {
            var lessons = new List<LessonSummaryView>();
            foreach (JsonElement lessonIdElement in module.GetProperty("lessonIds").EnumerateArray())
            {
                string lessonId = lessonIdElement.GetString()!;
                ContentCatalogItem item = catalog.FindById(lessonId)
                    ?? throw new InvalidDataException("Une leçon du parcours manque dans le catalogue publié.");
                if (item.Type != ContentDocumentType.Lesson)
                {
                    throw new InvalidDataException("Une entrée de leçon du parcours possède un type incorrect.");
                }

                string lessonManifestPath = ResolveLessonManifest(lessonId);
                string lessonJson = await ReadTextAsync(lessonManifestPath, cancellationToken);
                using JsonDocument lessonDocument = JsonDocument.Parse(lessonJson, JsonOptions);
                int estimatedMinutes = lessonDocument.RootElement
                    .GetProperty("estimatedMinutes")
                    .GetInt32();
                lessons.Add(new LessonSummaryView(
                    item.Id,
                    item.Title,
                    item.Summary,
                    estimatedMinutes,
                    item.Skills));
            }

            modules.Add(new CurriculumModuleView(
                module.GetProperty("id").GetString()!,
                module.GetProperty("title").GetString()!,
                Array.AsReadOnly(lessons.ToArray())));
        }

        return new LessonLibraryView(
            root.GetProperty("title").GetString()!,
            root.GetProperty("description").GetString()!,
            Array.AsReadOnly(modules.ToArray()));
    }

    public async ValueTask<LessonContentDocument?> GetLessonAsync(
        string lessonId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonId);
        ContentCatalogItem? catalogItem = _catalogProvider.Current.FindById(lessonId);
        if (catalogItem is null || catalogItem.Type != ContentDocumentType.Lesson)
        {
            return null;
        }

        string manifestPath = ResolveLessonManifest(lessonId);
        string json = await ReadTextAsync(manifestPath, cancellationToken);
        using JsonDocument document = JsonDocument.Parse(json, JsonOptions);
        JsonElement root = document.RootElement;
        if (!string.Equals(root.GetProperty("id").GetString(), lessonId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("L'identifiant du manifeste de leçon ne correspond pas à son chemin.");
        }

        string markdownPath = ResolveFile(
            root.GetProperty("markdownPath").GetString()!,
            Path.GetDirectoryName(manifestPath)!);
        string markdown = await ReadTextAsync(markdownPath, cancellationToken);
        LessonParsedMarkdown parsed = SafeMarkdownLessonParser.Parse(markdown);
        string[] activityIds = parsed.Sections
            .Select(section => $"section:{section.Id}")
            .Append($"quiz:{parsed.Quiz.PublicView.Id}")
            .ToArray();
        string[] objectives = root.GetProperty("objectives")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();
        string[] skills = root.GetProperty("skills")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!)
            .ToArray();
        var view = new LessonView(
            lessonId,
            root.GetProperty("version").GetInt32(),
            root.GetProperty("title").GetString()!,
            root.GetProperty("week").GetInt32(),
            root.GetProperty("estimatedMinutes").GetInt32(),
            Array.AsReadOnly(objectives),
            Array.AsReadOnly(skills),
            parsed.Sections,
            parsed.Quiz.PublicView,
            Array.AsReadOnly(activityIds));
        return new LessonContentDocument(view, parsed.Quiz);
    }

    private string ResolveLessonManifest(string lessonId) => ResolveFile(
        $"curriculum/lessons/{lessonId}/lesson.json",
        _catalogDirectory);

    private string ResolveFile(string relativePath, string allowedDirectory)
    {
        if (Path.IsPathFullyQualified(relativePath)
            || relativePath.Contains("..", StringComparison.Ordinal)
            || relativePath.Contains(':')
            || relativePath.Contains('\\'))
        {
            throw new InvalidDataException("Un chemin de contenu du lecteur est interdit.");
        }

        string fullPath = Path.GetFullPath(relativePath, allowedDirectory);
        EnsureContained(fullPath, allowedDirectory);
        EnsureContained(fullPath, _contentRoot);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Un fichier public de leçon est introuvable.", fullPath);
        }

        EnsureNoReparsePoint(fullPath);
        return fullPath;
    }

    private static void EnsureContained(string candidate, string root)
    {
        string canonicalRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string canonicalCandidate = Path.GetFullPath(candidate);
        if (!canonicalCandidate.Equals(canonicalRoot, PathComparison)
            && !canonicalCandidate.StartsWith($"{canonicalRoot}{Path.DirectorySeparatorChar}", PathComparison))
        {
            throw new InvalidDataException("Un chemin du lecteur sort de la racine de contenu autorisée.");
        }
    }

    private static void EnsureNoReparsePoint(string filePath)
    {
        for (FileSystemInfo? item = new FileInfo(filePath); item is not null; item = item switch
        {
            FileInfo file => file.Directory,
            DirectoryInfo directory => directory.Parent,
            _ => null,
        })
        {
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Les liens symboliques sont interdits dans le contenu du lecteur.");
            }
        }
    }

    private static async Task<string> ReadTextAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (info.Length is <= 0 or > MaximumFileBytes)
        {
            throw new InvalidDataException("Un fichier public de leçon possède une taille interdite.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        return StrictUtf8.GetString(bytes);
    }

    private static JsonDocumentOptions JsonOptions => new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64,
    };

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
