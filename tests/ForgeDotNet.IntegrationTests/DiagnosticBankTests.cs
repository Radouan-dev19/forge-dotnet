using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Infrastructure.Diagnostic;

namespace ForgeDotNet.IntegrationTests;

public sealed class DiagnosticBankTests
{
    [Fact]
    public async Task VersionedBankCoversEveryDomainAndKeepsAnswerKeyPrivate()
    {
        using var source = CreateSource(FindBankDirectory());

        DiagnosticBank bank = await source.GetAsync();
        DiagnosticScoringRubric rubric = await source.GetRubricAsync();

        Assert.Equal("forge-diagnostic-initial", bank.Id);
        Assert.Equal(1, bank.Version);
        Assert.Equal(36, bank.Questions.Count);
        Assert.Equal(64, bank.Revision.Length);
        Assert.Equal("forge-diagnostic-rubric", rubric.Snapshot.Id);
        Assert.Equal(1, rubric.Snapshot.Version);
        Assert.Equal(bank.Revision, rubric.Snapshot.BankRevision);
        Assert.Equal(36, rubric.ExpectedOptions.Count);
        Assert.Equal(5, rubric.Snapshot.DomainWeights.Count(domain => domain.IsCritical));
        foreach (DiagnosticDomain domain in DiagnosticDomains.All)
        {
            DiagnosticQuestion[] questions = bank.Questions.Where(question => question.Domain == domain).ToArray();
            Assert.Equal(4, questions.Length);
            Assert.Equal([1, 2, 2, 3], questions.Select(question => question.Difficulty).Order().ToArray());
        }

        Assert.DoesNotContain(typeof(DiagnosticQuestion).GetProperties(), property =>
            property.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Expected", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(IDiagnosticBankSource).GetMethods(), method =>
            method.Name.Contains("Answer", StringComparison.OrdinalIgnoreCase)
            || method.Name.Contains("Key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InvalidPrivateKeyFailsClosedWithoutPublishingBank()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "ForgeDotNet.DiagnosticBankTests", Guid.NewGuid().ToString("N"));
        string bankDirectory = Path.Combine(temporaryRoot, "diagnostic", "v1");
        Directory.CreateDirectory(bankDirectory);
        try
        {
            string sourceDirectory = FindBankDirectory();
            File.Copy(Path.Combine(sourceDirectory, "questions.json"), Path.Combine(bankDirectory, "questions.json"));
            File.Copy(Path.Combine(sourceDirectory, "rubric.json"), Path.Combine(bankDirectory, "rubric.json"));
            string key = await File.ReadAllTextAsync(Path.Combine(sourceDirectory, "answer-key.json"));
            key = key.Replace(
                "\"questionId\": \"diag-logic-d1-001\", \"expectedOptionId\": \"c\"",
                "\"questionId\": \"diag-logic-d1-001\", \"expectedOptionId\": \"unknown\"",
                StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(bankDirectory, "answer-key.json"), key);
            using var source = CreateSource(bankDirectory, temporaryRoot);

            await Assert.ThrowsAsync<InvalidDataException>(() => source.GetAsync().AsTask());
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidRubricFailsClosedBeforePublishingBankOrEvaluation()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "ForgeDotNet.DiagnosticRubricTests", Guid.NewGuid().ToString("N"));
        string bankDirectory = Path.Combine(temporaryRoot, "diagnostic", "v1");
        Directory.CreateDirectory(bankDirectory);
        try
        {
            string sourceDirectory = FindBankDirectory();
            File.Copy(Path.Combine(sourceDirectory, "questions.json"), Path.Combine(bankDirectory, "questions.json"));
            File.Copy(Path.Combine(sourceDirectory, "answer-key.json"), Path.Combine(bankDirectory, "answer-key.json"));
            string rubric = await File.ReadAllTextAsync(Path.Combine(sourceDirectory, "rubric.json"));
            rubric = rubric.Replace(
                "\"strongLowerBound\": 75",
                "\"strongLowerBound\": 40",
                StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(bankDirectory, "rubric.json"), rubric);
            using var source = CreateSource(bankDirectory, temporaryRoot);

            await Assert.ThrowsAsync<InvalidDataException>(() => source.GetAsync().AsTask());
            await Assert.ThrowsAsync<InvalidDataException>(() => source.GetRubricAsync().AsTask());
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    internal static FileSystemDiagnosticBankSource CreateSource(string bankDirectory, string? contentRoot = null) => new(
        new DiagnosticBankOptions
        {
            ContentRootPath = contentRoot ?? FindRepositoryRoot(),
            BankDirectoryPath = bankDirectory,
        });

    internal static string FindBankDirectory() => Path.Combine(
        FindRepositoryRoot(),
        "content",
        "diagnostic",
        "v1");

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Racine du dépôt de test introuvable.");
    }
}
