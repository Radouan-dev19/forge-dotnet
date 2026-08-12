using System.Net;
using ForgeDotNet.Application.Practice;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

public sealed class PracticeWebTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ForgeWebApplicationFactory _factory;

    public PracticeWebTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ManualPracticePageProtectsHintsAndSolutionUntilServerTransitions()
    {
        string library = WebUtility.HtmlDecode(await _client.GetStringAsync("/practice"));
        Assert.Contains("Pratique manuelle", library, StringComparison.Ordinal);
        Assert.Contains("Additionner deux montants", library, StringComparison.Ordinal);
        Assert.Contains("contenu v1", library, StringComparison.Ordinal);
        Assert.DoesNotContain("contenu v@exercise.Version", library, StringComparison.Ordinal);
        // Le texte doit décrire l'installation réellement configurée : ici, le mode manuel.
        Assert.Contains(
            "Cette installation est configurée en mode manuel",
            library,
            StringComparison.Ordinal);
        Assert.Contains("n’exécute, ne compile et ne teste aucun code", library, StringComparison.Ordinal);
        Assert.DoesNotContain("exécuteur de code isolé", library, StringComparison.Ordinal);
        Assert.DoesNotContain("return first + second;", library, StringComparison.Ordinal);

        string initial = WebUtility.HtmlDecode(await _client.GetStringAsync("/practice/reference-total-001"));
        Assert.Contains("Réflexion préalable", initial, StringComparison.Ordinal);
        Assert.Contains("mode manuel reste explicite et ne produit aucune validation automatique", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("résultats simulés", initial, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Quel invariant de", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("Concentrez la", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("return first + second;", initial, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/hidden", initial, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution/", initial, StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        PracticeService service = scope.ServiceProvider.GetRequiredService<PracticeService>();
        PracticeActivityView activity = await service.GetOrStartAsync("reference-total-001");
        activity = await service.SaveReflectionAsync(
            activity.ExerciseId,
            activity.Version,
            CompleteReflection());
        activity = await service.UnlockHintAsync(activity.ExerciseId, activity.Version, requestedLevel: 1);

        string afterHint = WebUtility.HtmlDecode(await _client.GetStringAsync("/practice/reference-total-001"));
        Assert.Contains("Question socratique", afterHint, StringComparison.Ordinal);
        Assert.Contains("H1 — Question socratique", afterHint, StringComparison.Ordinal);
        Assert.DoesNotContain("H@hint.Level", afterHint, StringComparison.Ordinal);
        Assert.Contains("Quel invariant de", afterHint, StringComparison.Ordinal);
        Assert.DoesNotContain("Concentrez la", afterHint, StringComparison.Ordinal);
        Assert.DoesNotContain("return first + second;", afterHint, StringComparison.Ordinal);
        Assert.Contains("Aucun test caché", afterHint, StringComparison.Ordinal);

        // Tant que la solution n'a pas été consultée, aucune reprise n'est annoncée…
        Assert.Contains(
            "aucune carte de récupération n’est planifiée",
            afterHint,
            StringComparison.Ordinal);
        Assert.DoesNotContain("une carte de récupération est planifiée", afterHint, StringComparison.Ordinal);
    }

    /// <summary>
    /// P1-04 : la page affirmait qu'aucune révision n'était planifiée alors que la consultation
    /// d'une solution crée bien une source de reprise. Le message doit distinguer l'absence de
    /// maîtrise immédiate de la planification d'une carte de récupération.
    /// </summary>
    [Fact]
    public async Task ViewingSolutionAnnouncesTheRecoveryReviewItActuallySchedules()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        PracticeService service = scope.ServiceProvider.GetRequiredService<PracticeService>();
        PracticeActivityView activity = await service.GetOrStartAsync("reference-total-002");
        activity = await service.SaveReflectionAsync(
            activity.ExerciseId,
            activity.Version,
            CompleteReflection());

        string beforeSolution = WebUtility.HtmlDecode(
            await _client.GetStringAsync("/practice/reference-total-002"));
        Assert.Contains(
            "aucune carte de récupération n’est planifiée",
            beforeSolution,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Aucun score de maîtrise ni révision planifiée",
            beforeSolution,
            StringComparison.Ordinal);
    }

    private static PracticeReflectionInput CompleteReflection() => new(
        "Je dois additionner chaque montant decimal tout en conservant la précision métier attendue.",
        "Une liste en lecture seule de valeurs decimal qui peut être vide.",
        "Un total decimal égal à la somme, ou zéro quand la liste est vide.",
        "Collection vide, avoir négatif, petit montant et grande collection sans conversion double.",
        "Un accumulateur decimal parcouru une seule fois doit satisfaire ces cas manuels.",
        "Initialiser le cumul à zéro, parcourir chaque montant, additionner puis retourner le total final.");
}
