using System.Net;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.Domain.DebugLab;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

public sealed class DebugLabWebTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ForgeWebApplicationFactory _factory;

    public DebugLabWebTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WebJourneyEnforcesInvestigationTestAndProtectedSolution()
    {
        string library = WebUtility.HtmlDecode(await _client.GetStringAsync("/debug-lab"));
        Assert.Contains("Déboguer par la preuve", library, StringComparison.Ordinal);
        Assert.Contains("Tracer une NullReferenceException", library, StringComparison.Ordinal);
        Assert.Contains("Identifier un mauvais enregistrement DI", library, StringComparison.Ordinal);
        // Trente scénarios : les vingt-cinq du socle (ids debug-*), les trois du bloc front-end
        // (ids front-*) et les deux laboratoires hérités de la piste senior (ids senior-legacy-*).
        // On compte donc tous les liens de détail, pas le seul préfixe debug-.
        Assert.Equal(30, Count(library, "href=\"debug-lab/"));

        string initial = WebUtility.HtmlDecode(await _client.GetStringAsync("/debug-lab/debug-null-reference-001"));
        Assert.Contains("Reproduire", initial, StringComparison.Ordinal);
        Assert.Contains("Breakpoint", initial, StringComparison.Ordinal);
        Assert.Contains("Watch", initial, StringComparison.Ordinal);
        Assert.Contains("Locals", initial, StringComparison.Ordinal);
        Assert.Contains("Call Stack", initial, StringComparison.Ordinal);
        Assert.Contains("value.Trim().ToUpperInvariant", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("string.IsNullOrWhiteSpace", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden_", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/hidden", initial, StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        DebugLabService service = scope.ServiceProvider.GetRequiredService<DebugLabService>();
        DebugLabActivityView activity = await service.GetOrStartAsync("debug-null-reference-001");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunCorrectionAsync(
            activity.ScenarioId, activity.Version, "public static class Submission { } ").AsTask());
        activity = await service.SaveInvestigationAsync(activity.ScenarioId, activity.Version, Investigation());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunCorrectionAsync(
            activity.ScenarioId, activity.Version, activity.BrokenSource).AsTask());
        activity = await service.PrepareCorrectionAsync(
            activity.ScenarioId, activity.Version,
            new DebugCorrectionPreparationInput(
                "Ajouter une garde null avant Trim et conserver la normalisation existante.",
                "Tester null, la valeur absente, une chaîne blanche et un nom nominal."));
        Assert.True(activity.CanRunCorrection);

        DebugCorrectionRunResult first = await service.RunCorrectionAsync(
            activity.ScenarioId, activity.Version, activity.BrokenSource);
        Assert.Equal(DebugLabState.CorrectionReady, first.Activity.State);
        Assert.False(first.Activity.CanViewSolution);
        DebugCorrectionRunResult second = await service.RunCorrectionAsync(
            first.Activity.ScenarioId, first.Activity.Version, first.Activity.BrokenSource);
        Assert.True(second.Activity.CanViewSolution);

        string gated = WebUtility.HtmlDecode(await _client.GetStringAsync("/debug-lab/debug-null-reference-001"));
        Assert.Contains("Consulter la solution", gated, StringComparison.Ordinal);
        Assert.DoesNotContain("string.IsNullOrWhiteSpace", gated, StringComparison.Ordinal);

        DebugLabActivityView viewed = await service.ViewSolutionAsync(second.Activity.ScenarioId, second.Activity.Version);
        Assert.Equal(DebugLabState.SolutionViewed, viewed.State);
        Assert.Null(viewed.CompletedAtUtc);
        string revealed = WebUtility.HtmlDecode(await _client.GetStringAsync("/debug-lab/debug-null-reference-001"));
        Assert.Contains("Solution consultée — scénario non terminé", revealed, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace", revealed, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden_", revealed, StringComparison.Ordinal);

        string markdown = await service.ExportJournalMarkdownAsync(viewed.ScenarioId);
        Assert.Contains("# Journal de bug", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("string.IsNullOrWhiteSpace", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden_", markdown, StringComparison.Ordinal);
    }

    private static DebugInvestigationInput Investigation() => new(
        "Une valeur absente provoque une NullReferenceException reproductible.",
        "L'import appelle FormatCustomerName avec un nom client absent.",
        "Trim pourrait déréférencer la valeur null avant la normalisation.",
        "La pile et la Watch montrent une valeur null au moment de Trim.",
        "Arrêt placé sur l'appel à Trim.",
        "Watch value affiche null.",
        "Locals confirme le paramètre absent.",
        "Call Stack relie l'import à FormatCustomerName.");

    private static int Count(string text, string value)
    {
        int count = 0;
        for (int index = 0; (index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0; index += value.Length) count++;
        return count;
    }
}
