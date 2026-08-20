using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

public sealed class FileSystemContentCatalogLoader : IContentCatalogLoader
{
    private readonly string _contentRootPath;
    private readonly IContentValidationService _contentValidator;

    public FileSystemContentCatalogLoader(
        IContentValidationService contentValidator,
        ContentValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(contentValidator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ContentRootPath);
        _contentValidator = contentValidator;
        _contentRootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
    }

    public async Task<ContentCatalogLoadResult> LoadAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ContentValidationReport validation = await _contentValidator
            .ValidateAsync(directoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return ContentCatalogLoadResult.Failure(validation.Issues);
        }

        string targetPath = Path.GetFullPath(directoryPath);
        var issues = new List<ContentValidationIssue>();
        List<StagedDocument> documents = ReadDocuments(targetPath, issues, cancellationToken);
        if (issues.Count > 0)
        {
            return ContentCatalogLoadResult.Failure(issues);
        }

        ContentValidationReport confirmation = await _contentValidator
            .ValidateAsync(directoryPath, cancellationToken)
            .ConfigureAwait(false);
        if (!confirmation.IsValid)
        {
            return ContentCatalogLoadResult.Failure(confirmation.Issues);
        }

        ValidateReferences(documents, issues);
        ValidatePrerequisiteGraphs(documents, issues);
        if (issues.Count > 0)
        {
            return ContentCatalogLoadResult.Failure(issues);
        }

        string revision = ComputeRevision(documents);
        var catalog = new ContentCatalog(revision, documents.Select(document => document.Item));
        return ContentCatalogLoadResult.Success(catalog);
    }

    private List<StagedDocument> ReadDocuments(
        string targetPath,
        ICollection<ContentValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var documents = new List<StagedDocument>();
        IEnumerable<string> candidates = Directory
            .EnumerateFiles(targetPath, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, PathComparer);
        foreach (string path in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string relativePath = Relative(path);
            if (ContentFileClassifier.IsIgnoredJson(relativePath))
            {
                continue;
            }

            ContentDocumentType? type = ContentFileClassifier.Classify(relativePath);
            if (type is null)
            {
                continue;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using JsonDocument json = JsonDocument.Parse(bytes, new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 64,
                });
                documents.Add(CreateStagedDocument(type.Value, relativePath, bytes, json.RootElement));
            }
            catch (JsonException exception)
            {
                string propertyPath = exception.Path ?? "$";
                AddIssue(issues, "catalog-json", relativePath, propertyPath, "Le manifeste a changé ou est devenu invalide pendant le chargement.");
            }
            catch (IOException)
            {
                AddIssue(issues, "catalog-read", relativePath, "$", "Lecture du manifeste impossible pendant le chargement.");
            }
            catch (UnauthorizedAccessException)
            {
                AddIssue(issues, "catalog-read", relativePath, "$", "Lecture du manifeste refusée pendant le chargement.");
            }
        }

        return documents;
    }

    private static StagedDocument CreateStagedDocument(
        ContentDocumentType type,
        string relativePath,
        byte[] rawBytes,
        JsonElement root)
    {
        string id = root.GetProperty("id").GetString()!;
        int version = root.GetProperty("version").GetInt32();
        string title = root.GetProperty("title").GetString()!;
        string[] skills = ReadSkills(type, root);
        string[] prerequisites = ReadStringArray(root, "prerequisites");
        string summary = ReadSummary(type, root);
        string[] glossary = ReadGlossary(type, root);
        var item = new ContentCatalogItem(
            id,
            version,
            type,
            title,
            summary,
            skills,
            prerequisites,
            glossary);
        var references = new List<CatalogReference>();
        AddPrerequisiteReferences(prerequisites, references);
        var modules = new List<StagedModule>();

        if (type == ContentDocumentType.Curriculum)
        {
            ReadCurriculumReferences(root, references, modules);
        }
        else if (type == ContentDocumentType.Exercise)
        {
            references.Add(new CatalogReference(
                root.GetProperty("variantId").GetString()!,
                ContentDocumentType.Exercise,
                "$.variantId"));
            references.Add(new CatalogReference(
                root.GetProperty("interviewQuestionId").GetString()!,
                ContentDocumentType.InterviewQuestion,
                "$.interviewQuestionId"));
        }
        else if (type == ContentDocumentType.Project)
        {
            string[] variantIds = ReadStringArray(root, "variantIds");
            for (int index = 0; index < variantIds.Length; index++)
            {
                references.Add(new CatalogReference(
                    variantIds[index],
                    ContentDocumentType.Project,
                    $"$.variantIds[{index}]"));
            }
        }

        return new StagedDocument(relativePath, rawBytes, item, references, modules);
    }

    private static void ReadCurriculumReferences(
        JsonElement root,
        ICollection<CatalogReference> references,
        ICollection<StagedModule> modules)
    {
        int moduleIndex = 0;
        foreach (JsonElement module in root.GetProperty("modules").EnumerateArray())
        {
            string moduleId = module.GetProperty("id").GetString()!;
            string[] modulePrerequisites = ReadStringArray(module, "prerequisites");
            modules.Add(new StagedModule(moduleId, moduleIndex, modulePrerequisites));

            string[] lessonIds = ReadStringArray(module, "lessonIds");
            for (int index = 0; index < lessonIds.Length; index++)
            {
                references.Add(new CatalogReference(
                    lessonIds[index],
                    ContentDocumentType.Lesson,
                    $"$.modules[{moduleIndex}].lessonIds[{index}]"));
            }

            string[] exerciseIds = ReadStringArray(module, "exerciseIds");
            for (int index = 0; index < exerciseIds.Length; index++)
            {
                references.Add(new CatalogReference(
                    exerciseIds[index],
                    ContentDocumentType.Exercise,
                    $"$.modules[{moduleIndex}].exerciseIds[{index}]"));
            }

            moduleIndex++;
        }
    }

    private static void AddPrerequisiteReferences(
        string[] prerequisites,
        ICollection<CatalogReference> references)
    {
        for (int index = 0; index < prerequisites.Length; index++)
        {
            references.Add(new CatalogReference(prerequisites[index], null, $"$.prerequisites[{index}]"));
        }
    }

    private static void ValidateReferences(
        IReadOnlyList<StagedDocument> documents,
        ICollection<ContentValidationIssue> issues)
    {
        Dictionary<string, StagedDocument> byId = documents.ToDictionary(
            document => document.Item.Id,
            StringComparer.Ordinal);
        foreach (StagedDocument document in documents.OrderBy(document => document.Item.Id, StringComparer.Ordinal))
        {
            foreach (CatalogReference reference in document.References)
            {
                if (!byId.TryGetValue(reference.TargetId, out StagedDocument? target))
                {
                    AddIssue(
                        issues,
                        "missing-reference",
                        document.RelativePath,
                        reference.PropertyPath,
                        $"Référence introuvable : {reference.TargetId}.");
                    continue;
                }

                if (reference.ExpectedType is not null && target.Item.Type != reference.ExpectedType.Value)
                {
                    AddIssue(
                        issues,
                        "reference-type",
                        document.RelativePath,
                        reference.PropertyPath,
                        $"La référence {reference.TargetId} doit cibler le type {reference.ExpectedType.Value}.");
                }

                if (reference.PropertyPath is "$.variantId" && reference.TargetId == document.Item.Id)
                {
                    AddIssue(
                        issues,
                        "self-reference",
                        document.RelativePath,
                        reference.PropertyPath,
                        "Une variante doit cibler un autre exercice.");
                }
            }

            ValidateModuleReferences(document, issues);
        }
    }

    private static void ValidateModuleReferences(
        StagedDocument document,
        ICollection<ContentValidationIssue> issues)
    {
        if (document.Modules.Count == 0)
        {
            return;
        }

        var moduleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedModule module in document.Modules)
        {
            if (!moduleIds.Add(module.Id))
            {
                AddIssue(
                    issues,
                    "duplicate-module-id",
                    document.RelativePath,
                    $"$.modules[{module.Index}].id",
                    $"Identifiant de module dupliqué : {module.Id}.");
            }
        }

        foreach (StagedModule module in document.Modules)
        {
            for (int index = 0; index < module.Prerequisites.Count; index++)
            {
                string prerequisite = module.Prerequisites[index];
                if (!moduleIds.Contains(prerequisite))
                {
                    AddIssue(
                        issues,
                        "missing-module-reference",
                        document.RelativePath,
                        $"$.modules[{module.Index}].prerequisites[{index}]",
                        $"Module prérequis introuvable dans ce parcours : {prerequisite}.");
                }
            }
        }
    }

    private static void ValidatePrerequisiteGraphs(
        IReadOnlyList<StagedDocument> documents,
        ICollection<ContentValidationIssue> issues)
    {
        var documentsById = documents.ToDictionary(document => document.Item.Id, StringComparer.Ordinal);
        var documentEdges = documents.ToDictionary(
            document => document.Item.Id,
            document => (IReadOnlyList<string>)document.Item.Prerequisites
                .Where(documentsById.ContainsKey)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
        foreach (GraphCycle cycle in FindCycles(documentEdges))
        {
            StagedDocument source = documentsById[cycle.SourceId];
            AddIssue(
                issues,
                "dependency-cycle",
                source.RelativePath,
                "$.prerequisites",
                $"Cycle de prérequis détecté : {string.Join(" -> ", cycle.Path)}.");
        }

        foreach (StagedDocument document in documents.Where(document => document.Modules.Count > 0))
        {
            var moduleIds = document.Modules.Select(module => module.Id).ToHashSet(StringComparer.Ordinal);
            var moduleEdges = document.Modules.ToDictionary(
                module => module.Id,
                module => (IReadOnlyList<string>)module.Prerequisites
                    .Where(moduleIds.Contains)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
            foreach (GraphCycle cycle in FindCycles(moduleEdges))
            {
                StagedModule source = document.Modules.Single(module => module.Id == cycle.SourceId);
                AddIssue(
                    issues,
                    "module-cycle",
                    document.RelativePath,
                    $"$.modules[{source.Index}].prerequisites",
                    $"Cycle de modules détecté : {string.Join(" -> ", cycle.Path)}.");
            }
        }
    }

    private static List<GraphCycle> FindCycles(
        IReadOnlyDictionary<string, IReadOnlyList<string>> edges)
    {
        var states = new Dictionary<string, VisitState>(StringComparer.Ordinal);
        var stack = new List<string>();
        var cycles = new List<GraphCycle>();
        var signatures = new HashSet<string>(StringComparer.Ordinal);

        foreach (string node in edges.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            Visit(node);
        }

        return cycles;

        void Visit(string node)
        {
            if (states.GetValueOrDefault(node) == VisitState.Visited)
            {
                return;
            }

            states[node] = VisitState.Visiting;
            stack.Add(node);
            foreach (string target in edges[node])
            {
                VisitState targetState = states.GetValueOrDefault(target);
                if (targetState == VisitState.Unvisited)
                {
                    Visit(target);
                }
                else if (targetState == VisitState.Visiting)
                {
                    int start = stack.IndexOf(target);
                    string[] cyclePath = stack.Skip(start).Append(target).ToArray();
                    string signature = CanonicalCycleSignature(cyclePath);
                    if (signatures.Add(signature))
                    {
                        cycles.Add(new GraphCycle(node, cyclePath));
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            states[node] = VisitState.Visited;
        }
    }

    private static string CanonicalCycleSignature(string[] closedPath)
    {
        string[] nodes = closedPath.Take(closedPath.Length - 1).ToArray();
        return nodes
            .Select((_, index) => string.Join('\u001F', nodes.Skip(index).Concat(nodes.Take(index))))
            .Min(StringComparer.Ordinal)!;
    }

    private static string[] ReadSkills(ContentDocumentType type, JsonElement root)
    {
        if (!root.TryGetProperty("skills", out JsonElement skills))
        {
            return [];
        }

        return type == ContentDocumentType.Lesson
            ? skills.EnumerateArray().Select(skill => skill.GetProperty("id").GetString()!).ToArray()
            : skills.EnumerateArray().Select(skill => skill.GetString()!).ToArray();
    }

    private static string ReadSummary(ContentDocumentType type, JsonElement root) => type switch
    {
        ContentDocumentType.Curriculum => root.GetProperty("description").GetString()!,
        ContentDocumentType.Lesson => JoinStrings(root.GetProperty("objectives")),
        ContentDocumentType.Exercise => $"{JoinStrings(root.GetProperty("constraints"))} {root.GetProperty("complexity").GetString()}",
        ContentDocumentType.DebugScenario => $"{root.GetProperty("expectedBehavior").GetString()} {JoinStrings(root.GetProperty("checklist"))}",
        ContentDocumentType.SqlScenario => JoinStrings(root.GetProperty("effectAssertions")),
        ContentDocumentType.InterviewQuestion => $"{root.GetProperty("question").GetString()} {JoinStrings(root.GetProperty("observableCriteria"))}",
        ContentDocumentType.EnglishActivity => $"{root.GetProperty("situation").GetString()} {JoinStrings(root.GetProperty("instructions"))}",
        ContentDocumentType.Project => string.Join(' ', root.GetProperty("milestones").EnumerateArray().Select(milestone =>
            $"{milestone.GetProperty("title").GetString()} {milestone.GetProperty("evidence").GetString()}")),

        // Sans ce cas, un laboratoire tomberait sur le résumé vide du défaut et resterait introuvable
        // par la recherche, ce qui reproduirait à l'échelle du catalogue le défaut qu'on corrige.
        ContentDocumentType.Lab => string.Join(' ', root.GetProperty("objectives").EnumerateArray().Select(objective =>
            $"{objective.GetProperty("goal").GetString()} {objective.GetProperty("observableProof").GetString()}")),

        // Un guide de carrière déclare son résumé : c'est lui qui alimente la recherche et l'index.
        ContentDocumentType.CareerGuide => root.GetProperty("summary").GetString()!,

        // Même contrat pour un guide du chapitre IA : le résumé déclaré nourrit l'index.
        ContentDocumentType.AiGuide => root.GetProperty("summary").GetString()!,
        _ => string.Empty,
    };

    private static string[] ReadGlossary(ContentDocumentType type, JsonElement root) =>
        type == ContentDocumentType.EnglishActivity
            ? root.GetProperty("vocabulary").EnumerateArray().Select(entry =>
                $"{entry.GetProperty("term").GetString()} {entry.GetProperty("meaning").GetString()}").ToArray()
            : [];

    private static string[] ReadStringArray(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement values)
            ? values.EnumerateArray().Select(value => value.GetString()!).ToArray()
            : [];

    private static string JoinStrings(JsonElement array) =>
        string.Join(' ', array.EnumerateArray().Select(value => value.GetString()));

    private static string ComputeRevision(IEnumerable<StagedDocument> documents)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (StagedDocument document in documents.OrderBy(document => document.RelativePath, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(document.RelativePath));
            hash.AppendData([0]);
            hash.AppendData(document.RawBytes);
            hash.AppendData([0]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private string Relative(string path) =>
        Path.GetRelativePath(_contentRootPath, path).Replace('\\', '/');

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static void AddIssue(
        ICollection<ContentValidationIssue> issues,
        string code,
        string filePath,
        string propertyPath,
        string message) => issues.Add(new ContentValidationIssue(code, filePath, propertyPath, message));

    private sealed record StagedDocument(
        string RelativePath,
        byte[] RawBytes,
        ContentCatalogItem Item,
        IReadOnlyList<CatalogReference> References,
        IReadOnlyList<StagedModule> Modules);

    private sealed record CatalogReference(
        string TargetId,
        ContentDocumentType? ExpectedType,
        string PropertyPath);

    private sealed record StagedModule(
        string Id,
        int Index,
        IReadOnlyList<string> Prerequisites);

    private sealed record GraphCycle(string SourceId, IReadOnlyList<string> Path);

    private enum VisitState
    {
        Unvisited,
        Visiting,
        Visited,
    }
}
