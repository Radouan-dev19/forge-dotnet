using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.UnitTests;

public sealed class DiagnosticSamplerTests
{
    [Fact]
    public void InitialPlanIsStableStratifiedAndCoversEveryDomain()
    {
        DiagnosticBank bank = CreateBank();

        DiagnosticPlan first = DiagnosticSampler.CreatePlan(bank, DiagnosticMode.Initial, seed: 42);
        DiagnosticPlan second = DiagnosticSampler.CreatePlan(bank, DiagnosticMode.Initial, seed: 42);

        Assert.Equal(27, first.QuestionCount);
        Assert.Equal(
            first.Sections.SelectMany(section => section.Questions).Select(question => question.Id),
            second.Sections.SelectMany(section => section.Questions).Select(question => question.Id));
        foreach (DiagnosticDomain domain in DiagnosticDomains.All)
        {
            DiagnosticQuestion[] questions = first.Sections
                .SelectMany(section => section.Questions)
                .Where(question => question.Domain == domain)
                .ToArray();
            Assert.Equal([1, 2, 3], questions.Select(question => question.Difficulty).Order().ToArray());
        }
    }

    [Fact]
    public void ReducedPlanCoversNineDomainsAndSeedCanSelectAnotherVariant()
    {
        DiagnosticBank bank = CreateBank();
        DiagnosticPlan first = DiagnosticSampler.CreatePlan(bank, DiagnosticMode.Reduced, seed: 10);
        DiagnosticPlan second = Enumerable.Range(11, 100)
            .Select(seed => DiagnosticSampler.CreatePlan(bank, DiagnosticMode.Reduced, seed))
            .First(plan => !plan.Sections
                .SelectMany(section => section.Questions)
                .Select(question => question.Id)
                .SequenceEqual(first.Sections.SelectMany(section => section.Questions).Select(question => question.Id)));

        Assert.Equal(9, first.QuestionCount);
        Assert.Equal(DiagnosticDomains.All.Order(), first.Sections
            .SelectMany(section => section.Questions)
            .Select(question => question.Domain)
            .Order());
        Assert.All(first.Sections.SelectMany(section => section.Questions), question =>
            Assert.Equal(2, question.Difficulty));
        Assert.NotEqual(
            first.Sections.SelectMany(section => section.Questions).Select(question => question.Id),
            second.Sections.SelectMany(section => section.Questions).Select(question => question.Id));
    }

    private static DiagnosticBank CreateBank()
    {
        var questions = new List<DiagnosticQuestion>();
        foreach (DiagnosticDomain domain in DiagnosticDomains.All)
        {
            questions.Add(CreateQuestion(domain, 1, 1));
            questions.Add(CreateQuestion(domain, 2, 1));
            questions.Add(CreateQuestion(domain, 2, 2));
            questions.Add(CreateQuestion(domain, 3, 1));
        }

        return new DiagnosticBank(
            "test-bank",
            1,
            new string('A', 64),
            "Banque de test",
            Array.AsReadOnly(questions.ToArray()));
    }

    private static DiagnosticQuestion CreateQuestion(DiagnosticDomain domain, int difficulty, int variant)
    {
        string id = $"{DiagnosticDomains.GetId(domain)}-{difficulty}-{variant}";
        return new DiagnosticQuestion(
            id,
            domain,
            difficulty,
            $"Question {id}",
            Array.AsReadOnly([
                new DiagnosticOption("a", "Option A"),
                new DiagnosticOption("b", "Option B"),
                new DiagnosticOption("c", "Option C"),
                new DiagnosticOption("d", "Option D"),
            ]));
    }
}
