using ForgeDotNet.Infrastructure.Content;

namespace ForgeDotNet.IntegrationTests;

public sealed class ContentValidationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ContentRoot = Path.Combine(RepositoryRoot, "content");

    [Fact]
    public async Task ValidFixturesCoverEveryV1DocumentType()
    {
        var validator = CreateValidator();

        var report = await validator.ValidateAsync(Path.Combine(ContentRoot, "fixtures", "valid"));

        Assert.True(report.IsValid, FormatIssues(report.Issues));
        Assert.Equal(8, report.DocumentsExamined);
        Assert.Equal(8, report.AcceptedDocuments);
    }

    [Fact]
    public async Task InvalidFixturesAggregateActionableErrorsWithoutPartialAcceptance()
    {
        var validator = CreateValidator();

        var report = await validator.ValidateAsync(Path.Combine(ContentRoot, "fixtures", "invalid"));

        Assert.False(report.IsValid);
        Assert.Equal(0, report.AcceptedDocuments);
        Assert.Contains(report.Issues, issue => issue.Code == "required");
        Assert.Contains(report.Issues, issue => issue.Code == "type");
        Assert.Contains(report.Issues, issue => issue.Code == "enum");
        Assert.Contains(report.Issues, issue => issue.Code == "duplicate-id");
        Assert.Contains(report.Issues, issue => issue.Code == "const" && issue.PropertyPath == "$.schemaVersion");
        Assert.Contains(report.Issues, issue => issue.Code == "minimum" && issue.PropertyPath == "$.version");
        Assert.Contains(report.Issues, issue => issue.Code == "maximum" && issue.PropertyPath.Contains("weight", StringComparison.Ordinal));
        Assert.Contains(report.Issues, issue => issue.Code == "path-traversal");
        Assert.Contains(report.Issues, issue => issue.Code == "minItems" && issue.PropertyPath == "$.sections");
        Assert.Contains(report.Issues, issue => issue.Code == "minItems" && issue.PropertyPath == "$.hints");
        Assert.All(report.Issues, issue =>
        {
            Assert.False(string.IsNullOrWhiteSpace(issue.FilePath));
            Assert.False(string.IsNullOrWhiteSpace(issue.PropertyPath));
            Assert.False(string.IsNullOrWhiteSpace(issue.Message));
        });
    }

    [Theory]
    [InlineData("unsubstituted-placeholder", "unsubstituted-placeholder")]
    [InlineData("hollow-lesson", "hollow-lesson")]
    [InlineData("cloned-content", "cloned-content")]
    public async Task AuthenticityFixtureIsRejectedByItsOwnRule(string fixtureName, string expectedCode)
    {
        var validator = CreateValidator();

        var report = await validator.ValidateAsync(
            Path.Combine(ContentRoot, "fixtures", "invalid", fixtureName));

        Assert.False(report.IsValid, FormatIssues(report.Issues));
        Assert.Equal(0, report.AcceptedDocuments);
        Assert.All(report.Issues, issue => Assert.Equal(expectedCode, issue.Code));
    }

    [Fact]
    public async Task GenericTypeCitedAsCodeIsNotMistakenForRawHtml()
    {
        string fixtureRoot = CreateTemporaryContentDirectory();
        string source = Path.Combine(ContentRoot, "fixtures", "valid", "curriculum", "lessons", "lesson-types-001");
        string destination = Path.Combine(fixtureRoot, "curriculum", "lessons", "generic-prose");
        CopyDirectory(source, destination);
        string manifestPath = Path.Combine(destination, "lesson.json");
        await File.WriteAllTextAsync(
            manifestPath,
            (await File.ReadAllTextAsync(manifestPath))
                .Replace("lesson-types-001", "generic-prose", StringComparison.Ordinal));
        await File.WriteAllTextAsync(
            Path.Combine(destination, "lesson.md"),
            "# Générique\n\nUne méthode retourne `IReadOnlyList<T>` et accepte `Func<int, bool>`.\n\n"
            + "```csharp\npublic static int Count<T>(IReadOnlyList<T> items) => items.Count;\n```\n");

        try
        {
            var report = await CreateValidator().ValidateAsync(fixtureRoot);

            Assert.DoesNotContain(report.Issues, issue => issue.Code == "raw-html");
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Fact]
    public async Task AuthenticityRulesStayInertOnContentThatDoesNotViolateThem()
    {
        var validator = CreateValidator();

        var report = await validator.ValidateAsync(Path.Combine(ContentRoot, "fixtures", "valid"));

        Assert.DoesNotContain(report.Issues, issue => ContentAuthenticityRules.IsAuthenticityCode(issue.Code));
    }

    [Fact]
    public async Task DirectoryOutsideContentIsRejectedBeforeReadingFiles()
    {
        var validator = CreateValidator();

        var report = await validator.ValidateAsync(RepositoryRoot);

        var issue = Assert.Single(report.Issues);
        Assert.Equal("path-outside-content", issue.Code);
        Assert.Equal(0, report.FilesExamined);
        Assert.Equal(0, report.AcceptedDocuments);
    }

    [Fact]
    public async Task OversizedManifestIsRejectedWithoutEchoingItsContent()
    {
        string fixtureRoot = CreateTemporaryContentDirectory();
        string interviewsDirectory = Path.Combine(fixtureRoot, "interviews");
        Directory.CreateDirectory(interviewsDirectory);
        string manifestPath = Path.Combine(interviewsDirectory, "oversized.json");
        const string marker = "SHOULD-NOT-APPEAR-IN-DIAGNOSTICS";
        await File.WriteAllTextAsync(manifestPath, $"{{\"secret\":\"{marker}{new string('x', 200)}\"}}");

        try
        {
            var validator = CreateValidator(maximumFileSizeBytes: 128);

            var report = await validator.ValidateAsync(fixtureRoot);

            Assert.Contains(report.Issues, issue => issue.Code == "file-too-large");
            Assert.DoesNotContain(marker, FormatIssues(report.Issues), StringComparison.Ordinal);
            Assert.Equal(0, report.AcceptedDocuments);
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RawHtmlInReferencedMarkdownIsRejected()
    {
        string fixtureRoot = CreateTemporaryContentDirectory();
        string source = Path.Combine(ContentRoot, "fixtures", "valid", "curriculum", "lessons", "lesson-types-001");
        string destination = Path.Combine(fixtureRoot, "curriculum", "lessons", "raw-html");
        CopyDirectory(source, destination);
        string manifestPath = Path.Combine(destination, "lesson.json");
        string manifest = (await File.ReadAllTextAsync(manifestPath))
            .Replace("lesson-types-001", "raw-html", StringComparison.Ordinal);
        await File.WriteAllTextAsync(manifestPath, manifest);
        await File.WriteAllTextAsync(Path.Combine(destination, "lesson.md"), "# Leçon\n\n<script>alert('x')</script>");

        try
        {
            var report = await CreateValidator().ValidateAsync(fixtureRoot);

            Assert.Contains(report.Issues, issue => issue.Code == "raw-html" && issue.PropertyPath == "$.markdownPath");
            Assert.Equal(0, report.AcceptedDocuments);
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SymbolicLinkIsRejectedWithoutFollowingItsTarget()
    {
        string fixtureRoot = CreateTemporaryContentDirectory();
        string externalTarget = Path.Combine(Path.GetTempPath(), $"forge-content-link-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(externalTarget);
        await File.WriteAllTextAsync(Path.Combine(externalTarget, "outside.json"), "{}");
        string linkPath = Path.Combine(fixtureRoot, "interviews");

        try
        {
            try
            {
                Directory.CreateSymbolicLink(linkPath, externalTarget);
            }
            catch (IOException) when (OperatingSystem.IsWindows())
            {
                // Windows exige le mode développeur ou un privilège dédié pour créer le lien.
                return;
            }

            var report = await CreateValidator().ValidateAsync(fixtureRoot);

            Assert.Contains(report.Issues, issue => issue.Code == "reparse-point");
            Assert.DoesNotContain(report.Issues, issue => issue.FilePath.EndsWith("outside.json", StringComparison.Ordinal));
            Assert.Equal(0, report.FilesExamined);
        }
        finally
        {
            if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath);
            }

            Directory.Delete(fixtureRoot, recursive: true);
            Directory.Delete(externalTarget, recursive: true);
        }
    }

    private static FileSystemContentValidationService CreateValidator(
        long maximumFileSizeBytes = ContentValidationOptions.DefaultMaximumFileSizeBytes) =>
        new(new ContentValidationOptions
        {
            ContentRootPath = ContentRoot,
            MaximumFileSizeBytes = maximumFileSizeBytes,
        });

    private static string CreateTemporaryContentDirectory()
    {
        string path = Path.Combine(ContentRoot, $".validation-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
    }

    private static string FormatIssues(IEnumerable<ForgeDotNet.Domain.Content.ContentValidationIssue> issues) =>
        string.Join(Environment.NewLine, issues.Select(issue =>
            $"{issue.FilePath} | {issue.PropertyPath} | {issue.Code} | {issue.Message}"));

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
