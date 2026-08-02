using System.Net;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Practice;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

public sealed class CodeRunnerWebTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ForgeWebApplicationFactory _factory;

    public CodeRunnerWebTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PracticePageShowsManualModeWithoutClaimingValidation()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        PracticeService practice = scope.ServiceProvider.GetRequiredService<PracticeService>();
        PracticeActivityView activity = await practice.GetOrStartAsync("reference-total-001");
        activity = await practice.SaveReflectionAsync(
            activity.ExerciseId,
            activity.Version,
            new PracticeReflectionInput(
                "Additionner les montants decimal sans perdre la précision attendue.",
                "Une collection de montants decimal, éventuellement vide.",
                "Le total exact, ou zéro lorsque la collection est vide.",
                "Collection vide, valeurs négatives et montant élevé.",
                "Un accumulateur decimal doit satisfaire les cas manuels annoncés.",
                "Initialiser le total, parcourir les montants, additionner puis retourner."));
        RunExercise runner = scope.ServiceProvider.GetRequiredService<RunExercise>();
        _ = await runner.ExecuteAsync(new RunExerciseCommand(
            Guid.NewGuid(),
            activity.ExerciseId,
            activity.ExerciseVersion,
            activity.ContentRevision,
            Array.AsReadOnly([
                new CodeRunSourceFile("Submission.cs", "public static decimal Total() => 0m;"),
            ])));

        string html = WebUtility.HtmlDecode(
            await _client.GetStringAsync("/practice/reference-total-001"));

        Assert.Contains("Compilation et tests isolés", html, StringComparison.Ordinal);
        Assert.Contains("Le code n’est jamais exécuté dans le processus Web", html, StringComparison.Ordinal);
        Assert.Contains("Exporter le ZIP manuel", html, StringComparison.Ordinal);
        Assert.Contains("Historique volatil des exécutions", html, StringComparison.Ordinal);
        Assert.Contains("Runner indisponible", html, StringComparison.Ordinal);
        Assert.Contains("Compilation", html, StringComparison.Ordinal);
        Assert.Contains("Indisponible", html, StringComparison.Ordinal);
        Assert.Contains("Tests", html, StringComparison.Ordinal);
        Assert.Contains("Non lancés", html, StringComparison.Ordinal);
        Assert.DoesNotContain("code validé", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tests/hidden", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution/", html, StringComparison.OrdinalIgnoreCase);
    }
}
