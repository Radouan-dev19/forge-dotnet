using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Les banques de quiz de l'onglet Thib doivent tenir la promesse faite au répondant : cinq
/// propositions identifiées a–e, une à quatre réponses justes, une explication substantielle, et
/// une distribution des bonnes réponses qui ne trahit pas la clé par sa forme.
/// </summary>
public sealed partial class TheoryQuizContentTests
{
    private static readonly string BankRoot = Path.Combine(FindRepositoryRoot(), "content", "reference", "theory-quiz");

    /// <summary>Instantané de volume : quatre banques de dix questions, senior ++ assumé.</summary>
    [Fact]
    public void TheBanksMatchTheFrozenVolume()
    {
        string[] banks = BankFiles();

        Assert.Equal(4, banks.Length);
        foreach (string path in banks)
        {
            using JsonDocument bank = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(10, bank.RootElement.GetProperty("questions").GetArrayLength());
            Assert.Equal(Path.GetFileNameWithoutExtension(path), bank.RootElement.GetProperty("id").GetString());
        }
    }

    [Fact]
    public void EveryQuestionOffersFiveDistinctOptionsAndAStrictKey()
    {
        var questionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in BankFiles())
        {
            using JsonDocument bank = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement question in bank.RootElement.GetProperty("questions").EnumerateArray())
            {
                string id = question.GetProperty("id").GetString()!;
                Assert.True(questionIds.Add(id), $"Identifiant de question dupliqué : {id}");

                string[] optionIds = question.GetProperty("options").EnumerateArray()
                    .Select(option => option.GetProperty("id").GetString()!).ToArray();
                Assert.Equal(["a", "b", "c", "d", "e"], optionIds);

                string[] texts = question.GetProperty("options").EnumerateArray()
                    .Select(option => option.GetProperty("text").GetString()!.Trim()).ToArray();
                Assert.Equal(texts.Length, texts.Distinct(StringComparer.OrdinalIgnoreCase).Count());

                string[] key = question.GetProperty("correctOptionIds").EnumerateArray()
                    .Select(value => value.GetString()!).ToArray();
                Assert.InRange(key.Length, 1, 4);
                Assert.All(key, optionId => Assert.Contains(optionId, optionIds));
                Assert.Equal(key.Length, key.Distinct(StringComparer.Ordinal).Count());

                // Une explication doit justifier les distracteurs, pas répéter la question.
                string rationale = question.GetProperty("rationale").GetString()!;
                Assert.True(rationale.Length >= 120, $"Explication trop courte : {id}");
                Assert.NotEqual(question.GetProperty("prompt").GetString(), rationale);
            }
        }
    }

    /// <summary>
    /// Une clé ne doit pas se deviner à la longueur : les bonnes réponses ne peuvent pas être
    /// systématiquement les propositions les plus longues, ni une lettre dominer la clé.
    /// </summary>
    [Fact]
    public void TheKeyCannotBeGuessedFromShapeAlone()
    {
        int longestIsCorrect = 0;
        int total = 0;
        var letterCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string path in BankFiles())
        {
            using JsonDocument bank = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement question in bank.RootElement.GetProperty("questions").EnumerateArray())
            {
                total++;
                var key = question.GetProperty("correctOptionIds").EnumerateArray()
                    .Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
                foreach (string letter in key)
                {
                    letterCounts[letter] = letterCounts.GetValueOrDefault(letter) + 1;
                }

                JsonElement longest = question.GetProperty("options").EnumerateArray()
                    .MaxBy(option => option.GetProperty("text").GetString()!.Length);
                if (key.Contains(longest.GetProperty("id").GetString()!))
                {
                    longestIsCorrect++;
                }
            }
        }

        Assert.True(total > 0);
        // Chaque lettre est juste dans au plus 90 % des questions et au moins 20 % : ni « toujours A »
        // ni « jamais E ».
        foreach (string letter in new[] { "a", "b", "c", "d", "e" })
        {
            int count = letterCounts.GetValueOrDefault(letter);
            Assert.InRange(count * 100 / total, 20, 90);
        }

        // La proposition la plus longue n'est pas juste dans plus de 85 % des questions.
        Assert.True(longestIsCorrect * 100 / total <= 85, $"La proposition la plus longue est juste dans {longestIsCorrect}/{total} questions.");
    }

    [Fact]
    public void NoQuestionLeaksAnAnswerInItsPromptOrTopic()
    {
        foreach (string path in BankFiles())
        {
            using JsonDocument bank = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement question in bank.RootElement.GetProperty("questions").EnumerateArray())
            {
                string prompt = question.GetProperty("prompt").GetString()!;
                Assert.DoesNotMatch(LetterHintRegex(), prompt);
                Assert.DoesNotContain("réponse :", prompt, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string[] BankFiles() =>
        Directory.GetFiles(BankRoot, "*.json", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal).ToArray();

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Racine du dépôt introuvable.");
    }

    [GeneratedRegex(@"\b(?:réponses?\s+)?[A-E]\s*(?:,|et)\s*[A-E]\b")]
    private static partial Regex LetterHintRegex();
}
