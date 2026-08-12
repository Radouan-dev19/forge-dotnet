using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.Infrastructure.Reviews;

/// <summary>
/// Lit la banque de cartes de révision du catalogue et la sert par exercice.
/// </summary>
/// <remarks>
/// La banque est chargée une fois, à la première demande, puis conservée en instantané immuable :
/// elle est en lecture seule et partagée par toutes les requêtes. Une banque absente ou illisible ne
/// fait pas échouer l'application — elle prive simplement la rétention espacée de cette source, ce
/// que le tableau de progression rend visible.
/// </remarks>
public sealed class CatalogReviewCardSource(PracticeContentOptions options) : IReviewCardSource, IDisposable
{
    private const string BankRelativePath = "reviews/exercise-review-cards.json";
    private const long MaximumBankBytes = 2_097_152;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private FrozenDictionary<string, IReadOnlyList<ExerciseReviewCard>>? _byExercise;

    public async ValueTask<IReadOnlyList<ExerciseReviewCard>> GetForExerciseAsync(
        string exerciseId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exerciseId))
        {
            return [];
        }

        FrozenDictionary<string, IReadOnlyList<ExerciseReviewCard>> bank =
            await GetBankAsync(cancellationToken);

        return bank.TryGetValue(exerciseId, out IReadOnlyList<ExerciseReviewCard>? cards) ? cards : [];
    }

    public void Dispose() => _loadGate.Dispose();

    private async ValueTask<FrozenDictionary<string, IReadOnlyList<ExerciseReviewCard>>> GetBankAsync(
        CancellationToken cancellationToken)
    {
        FrozenDictionary<string, IReadOnlyList<ExerciseReviewCard>>? cached = _byExercise;
        if (cached is not null)
        {
            return cached;
        }

        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            _byExercise ??= Load();
            return _byExercise;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private FrozenDictionary<string, IReadOnlyList<ExerciseReviewCard>> Load()
    {
        string path = Path.Combine(options.CatalogDirectoryPath, BankRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var empty = FrozenDictionary<string, IReadOnlyList<ExerciseReviewCard>>.Empty;
        if (!File.Exists(path) || new FileInfo(path).Length > MaximumBankBytes)
        {
            return empty;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(StrictUtf8.GetString(File.ReadAllBytes(path)));
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException or IOException)
        {
            return empty;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("cards", out JsonElement cards)
                || cards.ValueKind != JsonValueKind.Array)
            {
                return empty;
            }

            var byExercise = new Dictionary<string, List<ExerciseReviewCard>>(StringComparer.Ordinal);
            foreach (JsonElement card in cards.EnumerateArray())
            {
                ExerciseReviewCard? parsed = ReadCard(card);
                if (parsed is null)
                {
                    continue;
                }

                if (!byExercise.TryGetValue(parsed.ExerciseId, out List<ExerciseReviewCard>? list))
                {
                    list = [];
                    byExercise.Add(parsed.ExerciseId, list);
                }

                list.Add(parsed);
            }

            return byExercise.ToFrozenDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<ExerciseReviewCard>)Array.AsReadOnly(pair.Value.ToArray()),
                StringComparer.Ordinal);
        }
    }

    private static ExerciseReviewCard? ReadCard(JsonElement card)
    {
        if (card.ValueKind != JsonValueKind.Object
            || !TryReadString(card, "id", out string id)
            || !TryReadString(card, "exerciseId", out string exerciseId)
            || !TryReadString(card, "domain", out string domain)
            || !TryReadString(card, "question", out string question)
            || !TryReadString(card, "correctOptionId", out string correctOptionId)
            || !card.TryGetProperty("options", out JsonElement options)
            || options.ValueKind != JsonValueKind.Array
            || !TryMapDomain(domain, out MasteryDomain masteryDomain))
        {
            return null;
        }

        var parsedOptions = new List<ExerciseReviewOption>();
        foreach (JsonElement option in options.EnumerateArray())
        {
            if (option.ValueKind != JsonValueKind.Object
                || !TryReadString(option, "id", out string optionId)
                || !TryReadString(option, "text", out string text))
            {
                return null;
            }

            parsedOptions.Add(new ExerciseReviewOption(optionId, text));
        }

        // Une carte dont l'option attendue n'existe pas serait invérifiable : elle est écartée
        // plutôt que servie, faute de quoi aucune réponse ne pourrait jamais réussir.
        if (parsedOptions.Count < 2
            || !parsedOptions.Any(option => string.Equals(option.Id, correctOptionId, StringComparison.Ordinal)))
        {
            return null;
        }

        return new ExerciseReviewCard(
            id,
            exerciseId,
            masteryDomain,
            question,
            correctOptionId,
            Array.AsReadOnly(parsedOptions.ToArray()));
    }

    private static bool TryReadString(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(name, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        string? read = property.GetString();
        if (string.IsNullOrWhiteSpace(read))
        {
            return false;
        }

        value = read;
        return true;
    }

    private static bool TryMapDomain(string domain, out MasteryDomain masteryDomain)
    {
        (bool known, masteryDomain) = domain switch
        {
            "csharp" => (true, MasteryDomain.CSharp),
            "debugging" => (true, MasteryDomain.Debugging),
            "sql" => (true, MasteryDomain.Sql),
            "api" => (true, MasteryDomain.Api),
            "tests" => (true, MasteryDomain.Tests),
            "docker" => (true, MasteryDomain.Docker),
            "continuous-integration" => (true, MasteryDomain.ContinuousIntegration),
            "security" => (true, MasteryDomain.Security),
            "architecture" => (true, MasteryDomain.Architecture),
            "performance" => (true, MasteryDomain.Performance),
            "english" => (true, MasteryDomain.English),
            _ => (false, MasteryDomain.CSharp),
        };

        return known;
    }
}
