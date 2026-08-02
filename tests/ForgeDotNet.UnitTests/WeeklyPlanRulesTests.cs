using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.UnitTests;

public sealed class WeeklyPlanRulesTests
{
    [Fact]
    public void CriticalGapIsFirstAndAlwaysScheduledWithRemediation()
    {
        DiagnosticEvaluationReport evaluation = CreateEvaluation(
            scores: new Dictionary<DiagnosticDomain, decimal> { [DiagnosticDomain.Sql] = 0m });

        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            evaluation,
            CreateCurriculum(),
            profileAvailableHours: 12);

        WeeklyPlanRecommendation first = plan.Recommendations[0];
        Assert.Equal(DiagnosticDomain.Sql, first.Domain);
        Assert.Equal(WeeklyPlanRecommendationKind.CriticalRemediation, first.Kind);
        WeeklyPlanWeek sqlWeek = Assert.Single(
            plan.Weeks,
            week => week.Focuses.Any(focus => focus.Domain == DiagnosticDomain.Sql));
        Assert.True(sqlWeek.RemediationHours > 0m);
        Assert.Contains(
            sqlWeek.Focuses,
            focus => focus.Domain == DiagnosticDomain.Sql
                && focus.Depth == WeeklyPlanDepth.FullWithRemediation);
    }

    [Fact]
    public void StrongDomainShortensCoreStudyButKeepsKnowledgeCheck()
    {
        WeeklyPlanCurriculumSnapshot curriculum = CreateCurriculum();
        WeeklyPlanSnapshot strong = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(new Dictionary<DiagnosticDomain, decimal> { [DiagnosticDomain.CSharp] = 90m }),
            curriculum,
            profileAvailableHours: 10);
        WeeklyPlanSnapshot reinforcing = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(new Dictionary<DiagnosticDomain, decimal> { [DiagnosticDomain.CSharp] = 60m }),
            curriculum,
            profileAvailableHours: 10);

        WeeklyPlanWeek strongWeek = strong.Weeks.First(
            week => week.Focuses.Any(focus => focus.Domain == DiagnosticDomain.CSharp));
        WeeklyPlanWeek reinforcingWeek = reinforcing.Weeks.First(
            week => week.Focuses.Any(focus => focus.Domain == DiagnosticDomain.CSharp));
        Assert.True(strongWeek.CoreLearningHours < reinforcingWeek.CoreLearningHours);
        Assert.Equal(0m, strongWeek.RemediationHours);
        Assert.Equal(reinforcingWeek.KnowledgeCheckHours, strongWeek.KnowledgeCheckHours);
        Assert.True(strongWeek.KnowledgeCheckRequired);
        Assert.Contains(
            strongWeek.Focuses,
            focus => focus.Depth == WeeklyPlanDepth.CondensedWithVerification);
    }

    [Fact]
    public void LowAvailabilityProducesFeasibleLoadAndExplicitWarning()
    {
        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(),
            CreateCurriculum(),
            profileAvailableHours: 6);

        Assert.Equal(6, plan.TargetWeeklyHours);
        Assert.All(plan.Weeks, week => Assert.Equal(6, week.PlannedHours));
        Assert.Contains(plan.Warnings, warning => warning.Contains("inférieure", StringComparison.Ordinal));
    }

    [Fact]
    public void HighAvailabilityIsCappedAtFifteenHours()
    {
        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(),
            CreateCurriculum(),
            profileAvailableHours: 30);

        Assert.Equal(15, plan.TargetWeeklyHours);
        Assert.Contains(plan.Warnings, warning => warning.Contains("plafonnée", StringComparison.Ordinal));
    }

    [Fact]
    public void IncompleteDiagnosticCreatesProvisionalPlanWithoutPretendingWeakness()
    {
        DiagnosticEvaluationReport evaluation = CreateEvaluation(
            missingDomain: DiagnosticDomain.English,
            confidence: DiagnosticConfidence.Insufficient);

        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            evaluation,
            CreateCurriculum(),
            profileAvailableHours: 10);

        Assert.True(plan.IsProvisional);
        WeeklyPlanRecommendation english = Assert.Single(
            plan.Recommendations,
            item => item.Domain == DiagnosticDomain.English);
        Assert.Equal(WeeklyPlanRecommendationKind.EvidenceToCollect, english.Kind);
        Assert.Contains(plan.Warnings, warning => warning.Contains("provisoire", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CurriculumRejectsForwardPrerequisiteAsCycleRisk()
    {
        WeeklyPlanCurriculumSnapshot valid = CreateCurriculum();
        WeeklyPlanCurriculumWeek[] weeks = valid.Weeks.ToArray();
        weeks[1] = weeks[1] with { Prerequisites = Array.AsReadOnly(["week-24"]) };
        WeeklyPlanCurriculumSnapshot invalid = valid with { Weeks = Array.AsReadOnly(weeks) };

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            WeeklyPlanRules.ValidateCurriculum(invalid));

        Assert.Contains("cyclique", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AdjustmentCannotExceedAvailabilityOrPedagogicalCap()
    {
        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(),
            CreateCurriculum(),
            profileAvailableHours: 12);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyPlanRules.Reallocate(plan, profileAvailableHours: 12, requestedWeeklyHours: 13));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyPlanRules.Reallocate(plan, profileAvailableHours: 30, requestedWeeklyHours: 16));
    }

    [Fact]
    public void SnapshotRejectsAPlanThatRemovesACriticalGapFromItsCurriculumWeek()
    {
        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(
                scores: new Dictionary<DiagnosticDomain, decimal> { [DiagnosticDomain.CSharp] = 0m }),
            CreateCurriculum(),
            profileAvailableHours: 10);
        WeeklyPlanWeek[] weeks = plan.Weeks.ToArray();
        WeeklyPlanWeek csharpWeek = weeks.First(
            week => week.Focuses.Any(focus => focus.Domain == DiagnosticDomain.CSharp));
        weeks[csharpWeek.Number - 1] = csharpWeek with
        {
            Focuses = Array.AsReadOnly([
                csharpWeek.Focuses[0] with
                {
                    Domain = DiagnosticDomain.Logic,
                    DisplayName = DiagnosticDomains.GetDisplayName(DiagnosticDomain.Logic),
                },
            ]),
        };
        WeeklyPlanSnapshot bypassed = plan with { Weeks = Array.AsReadOnly(weeks) };

        Assert.Throws<InvalidDataException>(() => WeeklyPlanRules.ValidateSnapshot(bypassed));
    }

    [Fact]
    public void EveryWeekHasExactBoundedAllocationAndRequiredCheck()
    {
        WeeklyPlanSnapshot plan = WeeklyPlanRules.Create(
            Guid.NewGuid(),
            CreateEvaluation(),
            CreateCurriculum(),
            profileAvailableHours: 11);

        Assert.All(plan.Weeks, week =>
        {
            Assert.Equal(
                week.PlannedHours,
                week.CoreLearningHours + week.RemediationHours + week.ConsolidationHours + week.KnowledgeCheckHours);
            Assert.True(week.KnowledgeCheckRequired);
            Assert.True(week.KnowledgeCheckHours > 0m);
        });
    }

    private static WeeklyPlanCurriculumSnapshot CreateCurriculum()
    {
        DiagnosticDomain[] domains = DiagnosticDomains.All.ToArray();
        var weeks = new List<WeeklyPlanCurriculumWeek>();
        for (int number = 1; number <= 24; number++)
        {
            string id = $"week-{number:00}";
            DiagnosticDomain domain = number <= domains.Length
                ? domains[number - 1]
                : DiagnosticDomain.CSharp;
            weeks.Add(new WeeklyPlanCurriculumWeek(
                id,
                number,
                $"Semaine {number}",
                Array.AsReadOnly([domain]),
                number == 1
                    ? Array.Empty<string>()
                    : Array.AsReadOnly([$"week-{number - 1:00}"])));
        }

        return new WeeklyPlanCurriculumSnapshot(
            "curriculum",
            1,
            new string('C', 64),
            Array.AsReadOnly(weeks.ToArray()));
    }

    private static DiagnosticEvaluationReport CreateEvaluation(
        IReadOnlyDictionary<DiagnosticDomain, decimal>? scores = null,
        DiagnosticDomain? missingDomain = null,
        DiagnosticConfidence confidence = DiagnosticConfidence.Moderate)
    {
        scores ??= new Dictionary<DiagnosticDomain, decimal>();
        DiagnosticDomainWeight[] weights = DiagnosticDomains.All
            .Select(domain => new DiagnosticDomainWeight(domain, 1m, IsCritical(domain)))
            .ToArray();
        var rubric = new DiagnosticRubricSnapshot(
            "rubric",
            1,
            new string('A', 64),
            "bank",
            1,
            new string('B', 64),
            Array.AsReadOnly([
                new DiagnosticDifficultyWeight(1, 1m),
                new DiagnosticDifficultyWeight(2, 2m),
                new DiagnosticDifficultyWeight(3, 3m),
            ]),
            Array.AsReadOnly(weights),
            50m,
            35m,
            55m,
            75m,
            1.96d);
        DiagnosticDomainEvaluation[] domains = DiagnosticDomains.All
            .Select(domain =>
            {
                bool missing = domain == missingDomain;
                decimal score = scores.GetValueOrDefault(domain, 60m);
                return new DiagnosticDomainEvaluation(
                    domain,
                    IsCritical(domain),
                    PlannedQuestionCount: 3,
                    AnsweredQuestionCount: missing ? 0 : 3,
                    CorrectAnswerCount: missing ? 0 : score >= 75m ? 3 : score >= 50m ? 2 : 0,
                    new DiagnosticScoreInterval(score, missing ? 0m : score, missing ? 100m : score));
            })
            .ToArray();
        DiagnosticCriticalGap[] gaps = domains
            .Where(domain => domain.IsCritical
                && (domain.AnsweredQuestionCount == 0 || domain.Measure.Score < 50m))
            .Select(domain => new DiagnosticCriticalGap(
                domain.Domain,
                domain.AnsweredQuestionCount == 0
                    ? DiagnosticCriticalGapReason.MissingEvidence
                    : DiagnosticCriticalGapReason.ScoreBelowThreshold,
                domain.Measure.Score))
            .ToArray();
        var reliability = new DiagnosticReliability(
            CollectionComplete: missingDomain is null,
            AllDomainsObserved: missingDomain is null,
            FullInitialDepth: missingDomain is null,
            CoveragePercent: missingDomain is null ? 100m : 88.9m);
        return new DiagnosticEvaluationReport(
            rubric,
            DiagnosticMode.Initial,
            new DiagnosticScoreInterval(60m, 50m, 70m),
            confidence,
            gaps.Length > 0 ? DiagnosticLevel.FoundationsToStrengthen : DiagnosticLevel.Developing,
            reliability,
            Array.AsReadOnly(domains),
            Array.AsReadOnly(gaps));
    }

    private static bool IsCritical(DiagnosticDomain domain) => domain is
        DiagnosticDomain.CSharp
        or DiagnosticDomain.Debugging
        or DiagnosticDomain.Sql
        or DiagnosticDomain.Http
        or DiagnosticDomain.Testing;
}
