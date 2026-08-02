using System.Net;
namespace ForgeDotNet.EndToEndTests;

public sealed class WebSmokeTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WebSmokeTests(ForgeWebApplicationFactory factory)
    {
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
}
