using System.Net;
using System.Text.Json;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Domain.Exams;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

[Trait("Category", "ExamIntegrity")]
public sealed class ExamDashboardWebTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExamDashboardWebTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ActiveExamExposesNoAidOrReportAndDashboardUsesOnlyRealMetrics()
    {
        string initial = WebUtility.HtmlDecode(await _client.GetStringAsync("/exams"));
        Assert.Contains("Examen 1 — fondamentaux C# S1–S2", initial, StringComparison.Ordinal);
        Assert.Contains("Examen 4 — SQL et EF Core S8–S10", initial, StringComparison.Ordinal);
        Assert.Contains("Examen 5 — API et sécurité S11–S14", initial, StringComparison.Ordinal);
        Assert.Contains("Examen 6 — tests et qualité S15–S17", initial, StringComparison.Ordinal);
        Assert.Contains("Examen 7 — livraison S18–S20", initial, StringComparison.Ordinal);
        Assert.Contains("Examen 8 — Azure et observabilité S21–S22", initial, StringComparison.Ordinal);
        Assert.Contains("Examen 9 — synthèse et défense S1–S24", initial, StringComparison.Ordinal);
        Assert.Contains("contrôlées par le serveur", initial, StringComparison.Ordinal);
        Assert.Contains("ne prétend pas surveiller", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("solution/", initial, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tests/hidden", initial, StringComparison.OrdinalIgnoreCase);

        string emptyDashboard = WebUtility.HtmlDecode(await _client.GetStringAsync("/dashboard"));
        Assert.Contains("Temps actif observable", emptyDashboard, StringComparison.Ordinal);
        Assert.Contains("Indisponible", emptyDashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("série quotidienne", emptyDashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prêt pour l’emploi", emptyDashboard, StringComparison.OrdinalIgnoreCase);

        ExamAttemptView active;
        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            active = await scope.ServiceProvider.GetRequiredService<ExamService>()
                .StartAsync("reference-csharp-foundations-v1");
        }

        string activeJson = JsonSerializer.Serialize(active);
        Assert.Null(active.Report);
        Assert.DoesNotContain("DrawSeed", activeJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Solution", activeJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Expected", activeJson, StringComparison.OrdinalIgnoreCase);

        string practice = WebUtility.HtmlDecode(await _client.GetStringAsync($"/practice/{active.Items[0].ItemId}"));
        Assert.Contains("verrouillés pendant l’examen", practice, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution/", practice, StringComparison.OrdinalIgnoreCase);

        ExamAttemptView abandoned;
        await using (AsyncServiceScope scope = _factory.Services.CreateAsyncScope())
        {
            abandoned = await scope.ServiceProvider.GetRequiredService<ExamService>()
                .AbandonAsync(active.Id, active.Version);
        }

        Assert.Equal(ExamAttemptStatus.Abandoned, abandoned.Status);
        Assert.NotNull(abandoned.Report);
        Assert.False(abandoned.Report.Passed);
        Assert.Equal(64, abandoned.Report.DrawSeed.Length);

        string dashboard = WebUtility.HtmlDecode(await _client.GetStringAsync("/dashboard"));
        Assert.Contains("Abandonnés", dashboard, StringComparison.Ordinal);
        Assert.Contains(">1<", dashboard, StringComparison.Ordinal);
        Assert.Contains("Aucune moyenne ne compense", dashboard, StringComparison.Ordinal);
    }
}
