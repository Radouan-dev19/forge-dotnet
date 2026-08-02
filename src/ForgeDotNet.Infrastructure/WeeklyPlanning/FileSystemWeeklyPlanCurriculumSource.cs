using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.WeeklyPlanning;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.Infrastructure.WeeklyPlanning;

public sealed class FileSystemWeeklyPlanCurriculumSource(
    WeeklyPlanCurriculumOptions options) : IWeeklyPlanCurriculumSource, IDisposable
{
    private const int MaximumFileBytes = 65_536;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private WeeklyPlanCurriculumSnapshot? _cached;

    public void Dispose() => _gate.Dispose();

    public async ValueTask<WeeklyPlanCurriculumSnapshot> GetAsync(
        CancellationToken cancellationToken = default)
    {
        WeeklyPlanCurriculumSnapshot? cached = Volatile.Read(ref _cached);
        if (cached is not null)
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            cached = _cached;
            if (cached is not null)
            {
                return cached;
            }

            WeeklyPlanCurriculumSnapshot loaded = await LoadAsync(cancellationToken);
            Volatile.Write(ref _cached, loaded);
            return loaded;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<WeeklyPlanCurriculumSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        string contentRoot = Path.GetFullPath(options.ContentRootPath);
        string directory = Path.GetFullPath(options.DirectoryPath);
        string allowedPrefix = contentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!directory.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Le curriculum de planification doit rester sous content/.");
        }

        string path = Path.GetFullPath(options.FileName, directory);
        if (!path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(path))
        {
            throw new InvalidDataException("Le fichier du curriculum de planification est introuvable ou hors périmètre.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        if (bytes.Length == 0 || bytes.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("La taille du curriculum de planification est invalide.");
        }

        string json;
        try
        {
            json = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Le curriculum de planification doit être un fichier UTF-8 strict.", exception);
        }

        using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 16,
        });
        JsonElement root = document.RootElement;
        RequireExactProperties(root, ["id", "version", "weeks"], "$");
        string id = ReadRequiredString(root, "id", "$");
        int version = ReadPositiveInt(root, "version", "$");
        JsonElement weeksElement = root.GetProperty("weeks");
        if (weeksElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("$.weeks doit être un tableau.");
        }

        var weeks = new List<WeeklyPlanCurriculumWeek>();
        int index = 0;
        foreach (JsonElement weekElement in weeksElement.EnumerateArray())
        {
            string context = $"$.weeks[{index}]";
            RequireExactProperties(
                weekElement,
                ["id", "number", "title", "domains", "prerequisites"],
                context);
            weeks.Add(new WeeklyPlanCurriculumWeek(
                ReadRequiredString(weekElement, "id", context),
                ReadPositiveInt(weekElement, "number", context),
                ReadRequiredString(weekElement, "title", context),
                Array.AsReadOnly(ReadDomains(weekElement, context)),
                Array.AsReadOnly(ReadStrings(weekElement, "prerequisites", context))));
            index++;
        }

        string revision = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var snapshot = new WeeklyPlanCurriculumSnapshot(
            id,
            version,
            revision,
            Array.AsReadOnly(weeks.ToArray()));
        WeeklyPlanRules.ValidateCurriculum(snapshot);
        return snapshot;
    }

    private static DiagnosticDomain[] ReadDomains(JsonElement parent, string context)
    {
        string[] ids = ReadStrings(parent, "domains", context);
        var domains = new DiagnosticDomain[ids.Length];
        for (int index = 0; index < ids.Length; index++)
        {
            if (!DiagnosticDomains.TryParse(ids[index], out DiagnosticDomain domain))
            {
                throw new InvalidDataException($"{context}.domains[{index}] contient un domaine inconnu.");
            }

            domains[index] = domain;
        }

        return domains;
    }

    private static string[] ReadStrings(JsonElement parent, string propertyName, string context)
    {
        JsonElement element = parent.GetProperty(propertyName);
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{context}.{propertyName} doit être un tableau.");
        }

        return element.EnumerateArray()
            .Select((item, index) => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString())
                ? item.GetString()!
                : throw new InvalidDataException($"{context}.{propertyName}[{index}] doit être une chaîne non vide."))
            .ToArray();
    }

    private static string ReadRequiredString(JsonElement parent, string propertyName, string context)
    {
        JsonElement value = parent.GetProperty(propertyName);
        string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 160)
        {
            throw new InvalidDataException($"{context}.{propertyName} doit être une chaîne non vide de 160 caractères maximum.");
        }

        return text;
    }

    private static int ReadPositiveInt(JsonElement parent, string propertyName, string context)
    {
        JsonElement value = parent.GetProperty(propertyName);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number) || number < 1)
        {
            throw new InvalidDataException($"{context}.{propertyName} doit être un entier positif.");
        }

        return number;
    }

    private static void RequireExactProperties(
        JsonElement element,
        IReadOnlyCollection<string> expected,
        string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{context} doit être un objet JSON.");
        }

        string[] actual = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (actual.Length != expected.Count
            || actual.Distinct(StringComparer.Ordinal).Count() != actual.Length
            || actual.Except(expected, StringComparer.Ordinal).Any()
            || expected.Except(actual, StringComparer.Ordinal).Any())
        {
            throw new InvalidDataException($"{context} contient des propriétés absentes, inconnues ou dupliquées.");
        }
    }
}
