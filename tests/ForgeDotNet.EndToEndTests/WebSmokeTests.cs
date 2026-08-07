using System.Net;
using System.Text.RegularExpressions;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.Practice;
using Microsoft.Extensions.DependencyInjection;
namespace ForgeDotNet.EndToEndTests;

public sealed class WebSmokeTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WebSmokeTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/dashboard")]
    [InlineData("/profile")]
    [InlineData("/settings")]
    [InlineData("/about")]
    [InlineData("/learn")]
    [InlineData("/learn/reference-types-001")]
    [InlineData("/diagnostic")]
    [InlineData("/reviews")]
    [InlineData("/exams")]
    public async Task LocalPagesRespondSuccessfully(string path)
    {
        using var response = await _client.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 for '{path}', received {(int)response.StatusCode}. Body: {content}");
    }

    [Fact]
    public async Task HomeContainsTheProductNameAndNavigation()
    {
        var content = await _client.GetStringAsync("/");

        Assert.Contains("Forge.NET", content, StringComparison.Ordinal);
        Assert.Contains("Tableau de bord", content, StringComparison.Ordinal);
        Assert.Contains("Profil", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthReportsTheLocalProfileServiceAsHealthy()
    {
        using var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task BlazorBootstrapScriptIsAvailable()
    {
        using var response = await _client.GetAsync("/_framework/blazor.web.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Blazor", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SettingsExposeRealBackupAndRestoreControls()
    {
        var content = await _client.GetStringAsync("/settings");

        Assert.Contains("Créer la sauvegarde", content, StringComparison.Ordinal);
        Assert.Contains("Restaurer la sauvegarde", content, StringComparison.Ordinal);
        Assert.DoesNotContain("incrément futur", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownRouteReturnsNotFoundWithAUsefulPage()
    {
        using var response = await _client.GetAsync("/route-inexistante");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page introuvable", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LessonIsCompleteAccessibleAndDoesNotExposeServerOnlyContent()
    {
        using var response = await _client.GetAsync("/learn/reference-types-001");
        string content = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Objectif observable", content, StringComparison.Ordinal);
        Assert.Contains("Test de maîtrise", content, StringComparison.Ordinal);
        Assert.Contains("Sommaire", content, StringComparison.Ordinal);
        Assert.Contains("href=\"/learn/reference-types-001#resume\"", content, StringComparison.Ordinal);
        Assert.Contains("Ma note", content, StringComparison.Ordinal);
        Assert.Contains("Ajouter un signet", content, StringComparison.Ordinal);
        Assert.Contains("0 % — seules les sections confirmées", content, StringComparison.Ordinal);
        Assert.DoesNotContain("correct=1", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Correct : decimal", content, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/hidden", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution/", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>alert", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            response.Headers.GetValues("Content-Security-Policy"),
            value => value.Contains("default-src 'self'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchBySkillReturnsTheReferenceLesson()
    {
        string content = WebUtility.HtmlDecode(await _client.GetStringAsync("/learn?q=csharp.types"));

        Assert.Contains("Choisir un type monétaire adapté", content, StringComparison.Ordinal);
        Assert.Contains("csharp.types", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/dashboard")]
    [InlineData("/learn")]
    [InlineData("/practice")]
    [InlineData("/debug-lab")]
    [InlineData("/sql-lab")]
    [InlineData("/mastery")]
    [InlineData("/reviews")]
    [InlineData("/exams")]
    [InlineData("/diagnostic")]
    [InlineData("/profile")]
    [InlineData("/settings")]
    public async Task PublicPagesExposeKeyboardAndDocumentAccessibilityLandmarks(string path)
    {
        string content = await _client.GetStringAsync(path);

        Assert.Contains("<html lang=\"fr\">", content, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"viewport\"", content, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Navigation principale\"", content, StringComparison.Ordinal);
        Assert.Contains("class=\"skip-link\"", content, StringComparison.Ordinal);
        Assert.Contains("href=\"#main-content\"", content, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Count(content, "<main(?:\\s|>)", RegexOptions.IgnoreCase));
        Assert.Contains("id=\"main-content\"", content, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"-1\"", content, StringComparison.Ordinal);
        Assert.DoesNotMatch("tabindex=\\\"[1-9][0-9]*\\\"", content);
        Assert.DoesNotContain(" autofocus", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidatedContentSourcesReuseTheirImmutableSnapshots()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPracticeExerciseSource exercises = scope.ServiceProvider.GetRequiredService<IPracticeExerciseSource>();
        IExamBankSource exams = scope.ServiceProvider.GetRequiredService<IExamBankSource>();

        IReadOnlyList<ForgeDotNet.Domain.Practice.PracticeExercise> firstExercises =
            await exercises.ListAsync();
        IReadOnlyList<ForgeDotNet.Domain.Practice.PracticeExercise> secondExercises =
            await exercises.ListAsync();
        IReadOnlyList<ForgeDotNet.Domain.Exams.ExamBlueprint> firstExams = await exams.ListAsync();
        IReadOnlyList<ForgeDotNet.Domain.Exams.ExamBlueprint> secondExams = await exams.ListAsync();

        Assert.Same(firstExercises, secondExercises);
        Assert.Same(firstExams, secondExams);
        Assert.NotEmpty(firstExercises);
        Assert.NotEmpty(firstExams);
    }
}
