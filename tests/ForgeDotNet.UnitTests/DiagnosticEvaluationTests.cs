using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.UnitTests;

public sealed class DiagnosticEvaluationTests
{
    [Fact]
    public void AllCorrectInitialDiagnosticProducesBoundedStrongReport()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);

        DiagnosticEvaluationReport report = Evaluate(
            fixture,
            fixture.Plan.Sections.SelectMany(section => section.Questions));

        Assert.Equal(100m, report.Overall.Score);
        Assert.InRange(report.Overall.LowerBound, 75m, 100m);
        Assert.Equal(100m, report.Overall.UpperBound);
        Assert.Equal(DiagnosticConfidence.Moderate, report.Confidence);
        Assert.Equal(DiagnosticLevel.StrongToConfirm, report.Level);
        Assert.True(report.Reliability.CollectionComplete);
        Assert.True(report.Reliability.FullInitialDepth);
        Assert.Empty(report.CriticalGaps);
    }

    [Fact]
    public void AllWrongCannotBeRaisedByDomainWeights()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);
        DiagnosticEvaluationAnswer[] answers = fixture.Plan.Sections
            .SelectMany(section => section.Questions)
            .Select(question => new DiagnosticEvaluationAnswer(question.Id, "a"))
            .ToArray();

        DiagnosticEvaluationReport report = DiagnosticEvaluationRules.Evaluate(
            fixture.Plan,
            answers,
            fixture.Rubric);

        Assert.Equal(0m, report.Overall.Score);
        Assert.Equal(0m, report.Overall.LowerBound);
        Assert.InRange(report.Overall.UpperBound, 0m, 25m);
        Assert.Equal(DiagnosticLevel.FoundationsToStrengthen, report.Level);
        Assert.Equal(5, report.CriticalGaps.Count);
        Assert.All(report.CriticalGaps, gap =>
            Assert.Equal(DiagnosticCriticalGapReason.ScoreBelowThreshold, gap.Reason));
    }

    [Fact]
    public void MissingCriticalDomainMakesReportInsufficientAndExplicit()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);
        DiagnosticQuestion[] answered = fixture.Plan.Sections
            .SelectMany(section => section.Questions)
            .Where(question => question.Domain != DiagnosticDomain.Sql)
            .ToArray();

        DiagnosticEvaluationReport report = Evaluate(fixture, answered);

        Assert.False(report.Reliability.CollectionComplete);
        Assert.False(report.Reliability.AllDomainsObserved);
        Assert.Equal(DiagnosticConfidence.Insufficient, report.Confidence);
        Assert.Equal(DiagnosticLevel.EvidenceInsufficient, report.Level);
        DiagnosticCriticalGap gap = Assert.Single(report.CriticalGaps);
        Assert.Equal(DiagnosticDomain.Sql, gap.Domain);
        Assert.Equal(DiagnosticCriticalGapReason.MissingEvidence, gap.Reason);
        DiagnosticDomainEvaluation sql = Assert.Single(report.Domains, item => item.Domain == DiagnosticDomain.Sql);
        Assert.Equal(new DiagnosticScoreInterval(0m, 0m, 100m), sql.Measure);
    }

    [Fact]
    public void EasyAnswersAloneCannotInflateAnIncompleteInitialDiagnostic()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);
        DiagnosticQuestion[] easyQuestions = fixture.Plan.Sections
            .SelectMany(section => section.Questions)
            .Where(question => question.Difficulty == 1)
            .ToArray();

        DiagnosticEvaluationReport report = Evaluate(fixture, easyQuestions);

        Assert.Equal(16.7m, report.Overall.Score);
        Assert.Equal(33.3m, report.Reliability.CoveragePercent);
        Assert.Equal(DiagnosticConfidence.Insufficient, report.Confidence);
        Assert.Equal(DiagnosticLevel.EvidenceInsufficient, report.Level);
        Assert.True(report.Overall.UpperBound > report.Overall.Score);
    }

    [Fact]
    public void DifficultyWeightsGiveMoreEvidenceToAdvancedAnswers()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);
        DiagnosticQuestion[] questions = fixture.Plan.Sections.SelectMany(section => section.Questions).ToArray();

        DiagnosticEvaluationReport easy = Evaluate(
            fixture,
            questions.Where(question => question.Difficulty == 1));
        DiagnosticEvaluationReport advanced = Evaluate(
            fixture,
            questions.Where(question => question.Difficulty == 3));

        Assert.Equal(16.7m, easy.Overall.Score);
        Assert.Equal(50m, advanced.Overall.Score);
        Assert.True(advanced.Overall.Score > easy.Overall.Score);
    }

    [Fact]
    public void ReducedModeKeepsWiderUncertaintyAndLowConfidence()
    {
        EvaluationFixture reducedFixture = CreateFixture(DiagnosticMode.Reduced);
        EvaluationFixture initialFixture = CreateFixture(DiagnosticMode.Initial);

        DiagnosticEvaluationReport reduced = Evaluate(
            reducedFixture,
            reducedFixture.Plan.Sections.SelectMany(section => section.Questions));
        DiagnosticEvaluationReport initial = Evaluate(
            initialFixture,
            initialFixture.Plan.Sections.SelectMany(section => section.Questions));

        Assert.Equal(100m, reduced.Overall.Score);
        Assert.Equal(DiagnosticConfidence.Low, reduced.Confidence);
        Assert.Equal(DiagnosticLevel.Developing, reduced.Level);
        Assert.True(reduced.Overall.LowerBound < initial.Overall.LowerBound);
        Assert.True(reduced.IsProvisional);
    }

    [Fact]
    public void CriticalWeaknessCannotBeCompensatedByHighGlobalScore()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);
        DiagnosticEvaluationAnswer[] answers = fixture.Plan.Sections
            .SelectMany(section => section.Questions)
            .Select(question => new DiagnosticEvaluationAnswer(
                question.Id,
                question.Domain == DiagnosticDomain.Sql ? "a" : "b"))
            .ToArray();

        DiagnosticEvaluationReport report = DiagnosticEvaluationRules.Evaluate(
            fixture.Plan,
            answers,
            fixture.Rubric);

        Assert.True(report.Overall.Score > 80m);
        DiagnosticCriticalGap gap = Assert.Single(report.CriticalGaps);
        Assert.Equal(DiagnosticDomain.Sql, gap.Domain);
        Assert.Equal(DiagnosticLevel.FoundationsToStrengthen, report.Level);
    }

    [Fact]
    public void AnswerOrderDoesNotChangeScoresIntervalsOrGaps()
    {
        EvaluationFixture fixture = CreateFixture(DiagnosticMode.Initial);
        DiagnosticEvaluationAnswer[] answers = fixture.Plan.Sections
            .SelectMany(section => section.Questions)
            .Select((question, index) => new DiagnosticEvaluationAnswer(
                question.Id,
                index % 3 == 0 ? "a" : "b"))
            .ToArray();

        DiagnosticEvaluationReport forward = DiagnosticEvaluationRules.Evaluate(
            fixture.Plan,
            answers,
            fixture.Rubric);
        DiagnosticEvaluationReport reversed = DiagnosticEvaluationRules.Evaluate(
            fixture.Plan,
            answers.Reverse().ToArray(),
            fixture.Rubric);

        Assert.Equal(forward.Overall, reversed.Overall);
        Assert.Equal(forward.Confidence, reversed.Confidence);
        Assert.Equal(forward.Level, reversed.Level);
        Assert.Equal(
            forward.Domains.Select(domain => domain.Measure),
            reversed.Domains.Select(domain => domain.Measure));
        Assert.Equal(forward.CriticalGaps, reversed.CriticalGaps);
    }

    private static DiagnosticEvaluationReport Evaluate(
        EvaluationFixture fixture,
        IEnumerable<DiagnosticQuestion> correctQuestions)
    {
        DiagnosticEvaluationAnswer[] answers = correctQuestions
            .Select(question => new DiagnosticEvaluationAnswer(question.Id, "b"))
            .ToArray();
        return DiagnosticEvaluationRules.Evaluate(fixture.Plan, answers, fixture.Rubric);
    }

    private static EvaluationFixture CreateFixture(DiagnosticMode mode)
    {
        var questions = new List<DiagnosticQuestion>();
        var expected = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DiagnosticDomain domain in DiagnosticDomains.All)
        {
            foreach ((int difficulty, string variant) in new[] { (1, "a"), (2, "a"), (2, "b"), (3, "a") })
            {
                string id = $"{DiagnosticDomains.GetId(domain)}-{difficulty}-{variant}";
                questions.Add(new DiagnosticQuestion(
                    id,
                    domain,
                    difficulty,
                    $"Question {id}",
                    Array.AsReadOnly([
                        new DiagnosticOption("a", "Incorrecte"),
                        new DiagnosticOption("b", "Correcte"),
                        new DiagnosticOption("c", "Distracteur"),
                        new DiagnosticOption("d", "Distracteur"),
                    ])));
                expected.Add(id, "b");
            }
        }

        var bank = new DiagnosticBank(
            "bank",
            1,
            new string('B', 64),
            "Banque",
            Array.AsReadOnly(questions.ToArray()));
        DiagnosticPlan plan = DiagnosticSampler.CreatePlan(bank, mode, seed: 42);
        DiagnosticDomainWeight[] domainWeights =
        [
            new(DiagnosticDomain.Logic, 0.8m, false),
            new(DiagnosticDomain.CSharp, 1.2m, true),
            new(DiagnosticDomain.Reading, 0.9m, false),
            new(DiagnosticDomain.Debugging, 1.2m, true),
            new(DiagnosticDomain.Sql, 1.2m, true),
            new(DiagnosticDomain.Http, 1.1m, true),
            new(DiagnosticDomain.Git, 0.8m, false),
            new(DiagnosticDomain.Testing, 1.1m, true),
            new(DiagnosticDomain.English, 0.7m, false),
        ];
        var snapshot = new DiagnosticRubricSnapshot(
            "rubric",
            1,
            new string('A', 64),
            bank.Id,
            bank.Version,
            bank.Revision,
            Array.AsReadOnly([
                new DiagnosticDifficultyWeight(1, 1m),
                new DiagnosticDifficultyWeight(2, 2m),
                new DiagnosticDifficultyWeight(3, 3m),
            ]),
            Array.AsReadOnly(domainWeights),
            50m,
            35m,
            55m,
            75m,
            1.96d);
        return new EvaluationFixture(plan, new DiagnosticScoringRubric(snapshot, expected));
    }

    private sealed record EvaluationFixture(
        DiagnosticPlan Plan,
        DiagnosticScoringRubric Rubric);
}
