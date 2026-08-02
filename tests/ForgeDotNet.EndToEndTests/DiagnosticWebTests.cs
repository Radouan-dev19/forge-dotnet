using System.Net;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.WeeklyPlanning;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.WeeklyPlanning;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

public sealed class DiagnosticWebTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ForgeWebApplicationFactory _factory;

    public DiagnosticWebTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReducedDiagnosticCompletesWithAggregateEvaluationOnly()
    {
        string home = WebUtility.HtmlDecode(await _client.GetStringAsync("/diagnostic"));
        Assert.Contains("36 questions publiques", home, StringComparison.Ordinal);
        Assert.Contains("Logique", home, StringComparison.Ordinal);
        Assert.Contains("Anglais professionnel", home, StringComparison.Ordinal);
        Assert.Contains("rapport prudent", home, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sans conclure à une maîtrise ni générer de plan", home, StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        DiagnosticSessionService service = scope.ServiceProvider.GetRequiredService<DiagnosticSessionService>();
        DiagnosticSessionView session = await service.StartAsync(DiagnosticMode.Reduced);
        Assert.Equal(9, session.QuestionCount);
        Assert.Equal(9, session.Sections.Sum(section => section.QuestionCount));

        string firstPrompt = session.CurrentSection!.Questions[0].Prompt;
        string activePage = WebUtility.HtmlDecode(await _client.GetStringAsync($"/diagnostic/session/{session.Id}"));
        Assert.Contains("Temps restant", activePage, StringComparison.Ordinal);
        Assert.Contains(firstPrompt, activePage, StringComparison.Ordinal);
        Assert.DoesNotContain("expectedOptionId", activePage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer-key", activePage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Niveau estimé", activePage, StringComparison.OrdinalIgnoreCase);

        for (int sectionIndex = 0; sectionIndex < session.Sections.Count; sectionIndex++)
        {
            session = await service.GetAsync(session.Id);
            Assert.Equal(DiagnosticSectionStatus.Active, session.CurrentSection!.Status);
            foreach (DiagnosticQuestionView question in session.CurrentSection.Questions)
            {
                session = await service.SaveResponseAsync(
                    session.Id,
                    question.Id,
                    question.Options[0].Id);
            }

            session = await service.CompleteSectionAsync(session.Id, sectionIndex);
            if (sectionIndex < session.Sections.Count - 1)
            {
                session = await service.StartCurrentSectionAsync(session.Id);
            }
        }

        session = await service.FinishAsync(session.Id);
        Assert.True(session.IsComplete);
        Assert.Equal(DiagnosticSessionStatus.Completed, session.Status);

        string completedPage = WebUtility.HtmlDecode(await _client.GetStringAsync($"/diagnostic/session/{session.Id}"));
        Assert.Contains("Terminée — collecte complète", completedPage, StringComparison.Ordinal);
        Assert.Contains("Voir l'évaluation", completedPage, StringComparison.Ordinal);

        DiagnosticEvaluationService evaluations = scope.ServiceProvider.GetRequiredService<DiagnosticEvaluationService>();
        DiagnosticEvaluationView evaluation = await evaluations.GetOrCreateAsync(session.Id);
        string evaluationPage = WebUtility.HtmlDecode(await _client.GetStringAsync(
            $"/diagnostic/session/{session.Id}/evaluation"));
        Assert.Contains("Évaluation du diagnostic", evaluationPage, StringComparison.Ordinal);
        Assert.Contains($"{evaluation.Score:0.0} / 100", evaluationPage, StringComparison.Ordinal);
        Assert.Contains("confiance", evaluationPage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Carte des domaines", evaluationPage, StringComparison.Ordinal);
        Assert.DoesNotContain("expectedOptionId", evaluationPage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer-key", evaluationPage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(firstPrompt, evaluationPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Accepter le plan", evaluationPage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AbandonIsExplicitAndRemainsIncomplete()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        DiagnosticSessionService service = scope.ServiceProvider.GetRequiredService<DiagnosticSessionService>();
        DiagnosticSessionView session = await service.StartAsync(DiagnosticMode.Reduced);

        session = await service.AbandonAsync(session.Id);

        Assert.Equal(DiagnosticSessionStatus.Abandoned, session.Status);
        Assert.Equal(DiagnosticSectionStatus.Interrupted, session.Sections[0].Status);
        Assert.False(session.IsComplete);
        string page = WebUtility.HtmlDecode(await _client.GetStringAsync($"/diagnostic/session/{session.Id}"));
        Assert.Contains("Abandonnée — collecte incomplète", page, StringComparison.Ordinal);
        Assert.Contains("Voir l'évaluation", page, StringComparison.Ordinal);
        string evaluationPage = WebUtility.HtmlDecode(await _client.GetStringAsync(
            $"/diagnostic/session/{session.Id}/evaluation"));
        Assert.Contains("Rapport provisoire", evaluationPage, StringComparison.Ordinal);
        Assert.Contains("preuves insuffisantes", evaluationPage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plan personnalisé est une proposition distincte", evaluationPage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProvisionalPlanCanBeAdjustedAcceptedAndReloaded()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        DiagnosticSessionService sessions = scope.ServiceProvider.GetRequiredService<DiagnosticSessionService>();
        DiagnosticSessionView active = await sessions.StartAsync(DiagnosticMode.Reduced);
        DiagnosticSessionView abandoned = await sessions.AbandonAsync(active.Id);
        DiagnosticEvaluationService evaluations = scope.ServiceProvider.GetRequiredService<DiagnosticEvaluationService>();
        _ = await evaluations.GetOrCreateAsync(abandoned.Id);

        string evaluationPage = WebUtility.HtmlDecode(await _client.GetStringAsync(
            $"/diagnostic/session/{abandoned.Id}/evaluation"));
        Assert.Contains("Créer ou voir le plan", evaluationPage, StringComparison.Ordinal);

        WeeklyPlanService plans = scope.ServiceProvider.GetRequiredService<WeeklyPlanService>();
        WeeklyPlanView initial = await plans.GetOrCreateAsync(abandoned.Id);
        string initialPage = WebUtility.HtmlDecode(await _client.GetStringAsync($"/plan/{abandoned.Id}"));
        Assert.Contains("Plan personnalisé", initialPage, StringComparison.Ordinal);
        Assert.Contains("Plan provisoire", initialPage, StringComparison.Ordinal);
        Assert.Contains("24 semaines", initialPage, StringComparison.Ordinal);
        Assert.Contains("Accepter cette version", initialPage, StringComparison.Ordinal);
        Assert.Contains("Contrôle conservé", initialPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Lancer un exercice", initialPage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score de maîtrise calculé", initialPage, StringComparison.OrdinalIgnoreCase);

        WeeklyPlanView adjusted = await plans.AdjustHoursAsync(abandoned.Id, initial.Version, 8);
        string adjustedPage = WebUtility.HtmlDecode(await _client.GetStringAsync($"/plan/{abandoned.Id}"));
        Assert.Contains("Version 2", adjustedPage, StringComparison.Ordinal);
        Assert.Contains("8 h/semaine", adjustedPage, StringComparison.Ordinal);

        WeeklyPlanView accepted = await plans.AcceptAsync(abandoned.Id, adjusted.Version);
        string acceptedPage = WebUtility.HtmlDecode(await _client.GetStringAsync($"/plan/{abandoned.Id}"));
        Assert.Equal(WeeklyPlanStatus.Accepted, accepted.Status);
        Assert.Contains("Plan accepté", acceptedPage, StringComparison.Ordinal);
        Assert.Contains("Version 2 acceptée", acceptedPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Accepter cette version", acceptedPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Créer une nouvelle version", acceptedPage, StringComparison.Ordinal);
    }
}
