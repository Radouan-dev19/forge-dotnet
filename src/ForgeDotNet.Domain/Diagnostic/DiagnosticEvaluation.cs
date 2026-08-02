namespace ForgeDotNet.Domain.Diagnostic;

public sealed record DiagnosticDifficultyWeight(int Difficulty, decimal Weight);

public sealed record DiagnosticDomainWeight(
    DiagnosticDomain Domain,
    decimal Weight,
    bool IsCritical);

public sealed record DiagnosticRubricSnapshot(
    string Id,
    int Version,
    string Revision,
    string BankId,
    int BankVersion,
    string BankRevision,
    IReadOnlyList<DiagnosticDifficultyWeight> DifficultyWeights,
    IReadOnlyList<DiagnosticDomainWeight> DomainWeights,
    decimal CriticalGapScoreThreshold,
    decimal DevelopingLowerBound,
    decimal OperationalLowerBound,
    decimal StrongLowerBound,
    double WilsonZ);

public sealed record DiagnosticScoringRubric(
    DiagnosticRubricSnapshot Snapshot,
    IReadOnlyDictionary<string, string> ExpectedOptions);

public sealed record DiagnosticEvaluationAnswer(
    string QuestionId,
    string SelectedOptionId);

public enum DiagnosticConfidence
{
    Insufficient,
    Low,
    Moderate,
}

public enum DiagnosticLevel
{
    EvidenceInsufficient,
    FoundationsToStrengthen,
    Developing,
    OperationalToConfirm,
    StrongToConfirm,
}

public enum DiagnosticCriticalGapReason
{
    MissingEvidence,
    ScoreBelowThreshold,
}

public sealed record DiagnosticScoreInterval(
    decimal Score,
    decimal LowerBound,
    decimal UpperBound);

public sealed record DiagnosticDomainEvaluation(
    DiagnosticDomain Domain,
    bool IsCritical,
    int PlannedQuestionCount,
    int AnsweredQuestionCount,
    int CorrectAnswerCount,
    DiagnosticScoreInterval Measure);

public sealed record DiagnosticCriticalGap(
    DiagnosticDomain Domain,
    DiagnosticCriticalGapReason Reason,
    decimal Score);

public sealed record DiagnosticReliability(
    bool CollectionComplete,
    bool AllDomainsObserved,
    bool FullInitialDepth,
    decimal CoveragePercent);

public sealed record DiagnosticEvaluationReport(
    DiagnosticRubricSnapshot Rubric,
    DiagnosticMode Mode,
    DiagnosticScoreInterval Overall,
    DiagnosticConfidence Confidence,
    DiagnosticLevel Level,
    DiagnosticReliability Reliability,
    IReadOnlyList<DiagnosticDomainEvaluation> Domains,
    IReadOnlyList<DiagnosticCriticalGap> CriticalGaps)
{
    public bool IsProvisional => Confidence != DiagnosticConfidence.Moderate;
}

public static class DiagnosticEvaluationRules
{
    public static DiagnosticEvaluationReport Evaluate(
        DiagnosticPlan plan,
        IReadOnlyCollection<DiagnosticEvaluationAnswer> answers,
        DiagnosticScoringRubric rubric)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(rubric);
        ValidateRubric(rubric);

        DiagnosticQuestion[] questions = plan.Sections
            .SelectMany(section => section.Questions)
            .OrderBy(question => question.Id, StringComparer.Ordinal)
            .ToArray();
        if (questions.Length == 0 || questions.Select(question => question.Id).Distinct(StringComparer.Ordinal).Count() != questions.Length)
        {
            throw new InvalidDataException("Le plan de diagnostic à évaluer est invalide.");
        }

        var questionsById = questions.ToDictionary(question => question.Id, StringComparer.Ordinal);
        var answersById = new Dictionary<string, DiagnosticEvaluationAnswer>(StringComparer.Ordinal);
        foreach (DiagnosticEvaluationAnswer answer in answers)
        {
            if (!answersById.TryAdd(answer.QuestionId, answer))
            {
                throw new InvalidDataException("Une réponse de diagnostic est dupliquée.");
            }

            if (!questionsById.TryGetValue(answer.QuestionId, out DiagnosticQuestion? question)
                || !question.Options.Any(option => string.Equals(option.Id, answer.SelectedOptionId, StringComparison.Ordinal)))
            {
                throw new InvalidDataException("Une réponse ne correspond pas au plan figé.");
            }
        }

        var difficultyWeights = rubric.Snapshot.DifficultyWeights.ToDictionary(item => item.Difficulty, item => item.Weight);
        var domainWeights = rubric.Snapshot.DomainWeights.ToDictionary(item => item.Domain);
        var observations = new List<WeightedObservation>(questions.Length);
        foreach (DiagnosticQuestion question in questions)
        {
            if (!rubric.ExpectedOptions.TryGetValue(question.Id, out string? expectedOptionId)
                || !question.Options.Any(option => string.Equals(option.Id, expectedOptionId, StringComparison.Ordinal)))
            {
                throw new InvalidDataException("Le barème ne couvre pas le plan figé.");
            }

            bool answered = answersById.TryGetValue(question.Id, out DiagnosticEvaluationAnswer? answer);
            observations.Add(new WeightedObservation(
                question.Domain,
                difficultyWeights[question.Difficulty],
                domainWeights[question.Domain].Weight,
                answered,
                answered && string.Equals(answer!.SelectedOptionId, expectedOptionId, StringComparison.Ordinal)));
        }

        DiagnosticDomainEvaluation[] domains = DiagnosticDomains.All
            .Select(domain => CreateDomainEvaluation(
                domain,
                domainWeights[domain].IsCritical,
                observations.Where(observation => observation.Domain == domain).ToArray(),
                rubric.Snapshot.WilsonZ))
            .ToArray();
        DiagnosticScoreInterval overall = CreateMeasure(
            observations.Select(observation => observation with
            {
                DifficultyWeight = observation.DifficultyWeight * observation.DomainWeight,
                DomainWeight = 1m,
            }).ToArray(),
            rubric.Snapshot.WilsonZ);

        int answeredCount = observations.Count(observation => observation.Answered);
        bool allDomainsObserved = domains.All(domain => domain.AnsweredQuestionCount > 0);
        bool collectionComplete = answeredCount == questions.Length;
        bool fullInitialDepth = plan.Mode == DiagnosticMode.Initial
            && domains.All(domain => domain.PlannedQuestionCount >= 3 && domain.AnsweredQuestionCount >= 3);
        var reliability = new DiagnosticReliability(
            collectionComplete,
            allDomainsObserved,
            fullInitialDepth,
            RoundPercent(100m * answeredCount / questions.Length));
        DiagnosticConfidence confidence = !collectionComplete || !allDomainsObserved
            ? DiagnosticConfidence.Insufficient
            : plan.Mode == DiagnosticMode.Reduced
                ? DiagnosticConfidence.Low
                : DiagnosticConfidence.Moderate;
        DiagnosticCriticalGap[] criticalGaps = domains
            .Where(domain => domain.IsCritical
                && (domain.AnsweredQuestionCount == 0
                    || domain.Measure.Score < rubric.Snapshot.CriticalGapScoreThreshold))
            .Select(domain => new DiagnosticCriticalGap(
                domain.Domain,
                domain.AnsweredQuestionCount == 0
                    ? DiagnosticCriticalGapReason.MissingEvidence
                    : DiagnosticCriticalGapReason.ScoreBelowThreshold,
                domain.Measure.Score))
            .ToArray();
        DiagnosticLevel level = DetermineLevel(
            overall.LowerBound,
            confidence,
            criticalGaps.Length > 0,
            rubric.Snapshot);

        return new DiagnosticEvaluationReport(
            rubric.Snapshot,
            plan.Mode,
            overall,
            confidence,
            level,
            reliability,
            Array.AsReadOnly(domains),
            Array.AsReadOnly(criticalGaps));
    }

    private static DiagnosticDomainEvaluation CreateDomainEvaluation(
        DiagnosticDomain domain,
        bool isCritical,
        IReadOnlyCollection<WeightedObservation> observations,
        double z) => new(
            domain,
            isCritical,
            observations.Count,
            observations.Count(observation => observation.Answered),
            observations.Count(observation => observation.Correct),
            CreateMeasure(observations, z));

    private static DiagnosticScoreInterval CreateMeasure(
        IReadOnlyCollection<WeightedObservation> observations,
        double z)
    {
        decimal plannedWeight = observations.Sum(observation => observation.DifficultyWeight);
        decimal answeredWeight = observations.Where(observation => observation.Answered).Sum(observation => observation.DifficultyWeight);
        decimal earnedWeight = observations.Where(observation => observation.Correct).Sum(observation => observation.DifficultyWeight);
        if (plannedWeight <= 0m)
        {
            throw new InvalidDataException("Le poids planifié du diagnostic est invalide.");
        }

        decimal score = RoundPercent(100m * earnedWeight / plannedWeight);
        if (answeredWeight == 0m)
        {
            return new DiagnosticScoreInterval(score, 0m, 100m);
        }

        decimal squaredWeights = observations
            .Where(observation => observation.Answered)
            .Sum(observation => observation.DifficultyWeight * observation.DifficultyWeight);
        double effectiveSampleSize = (double)(answeredWeight * answeredWeight / squaredWeights);
        double proportion = (double)(earnedWeight / answeredWeight);
        (double lower, double upper) = Wilson(proportion, effectiveSampleSize, z);
        decimal lowerBound = 100m * (decimal)lower * answeredWeight / plannedWeight;
        decimal upperBound = 100m * (((decimal)upper * answeredWeight) + (plannedWeight - answeredWeight)) / plannedWeight;
        return new DiagnosticScoreInterval(
            score,
            RoundPercent(decimal.Clamp(lowerBound, 0m, 100m)),
            RoundPercent(decimal.Clamp(upperBound, 0m, 100m)));
    }

    private static (double Lower, double Upper) Wilson(double proportion, double sampleSize, double z)
    {
        double zSquared = z * z;
        double denominator = 1d + (zSquared / sampleSize);
        double centre = (proportion + (zSquared / (2d * sampleSize))) / denominator;
        double margin = z * Math.Sqrt(
            ((proportion * (1d - proportion)) / sampleSize)
            + (zSquared / (4d * sampleSize * sampleSize))) / denominator;
        return (Math.Max(0d, centre - margin), Math.Min(1d, centre + margin));
    }

    private static DiagnosticLevel DetermineLevel(
        decimal lowerBound,
        DiagnosticConfidence confidence,
        bool hasCriticalGap,
        DiagnosticRubricSnapshot rubric)
    {
        if (confidence == DiagnosticConfidence.Insufficient)
        {
            return DiagnosticLevel.EvidenceInsufficient;
        }

        if (hasCriticalGap)
        {
            return DiagnosticLevel.FoundationsToStrengthen;
        }

        if (confidence == DiagnosticConfidence.Low)
        {
            return lowerBound >= rubric.DevelopingLowerBound
                ? DiagnosticLevel.Developing
                : DiagnosticLevel.FoundationsToStrengthen;
        }

        if (lowerBound >= rubric.StrongLowerBound)
        {
            return DiagnosticLevel.StrongToConfirm;
        }

        if (lowerBound >= rubric.OperationalLowerBound)
        {
            return DiagnosticLevel.OperationalToConfirm;
        }

        return lowerBound >= rubric.DevelopingLowerBound
            ? DiagnosticLevel.Developing
            : DiagnosticLevel.FoundationsToStrengthen;
    }

    private static void ValidateRubric(DiagnosticScoringRubric rubric)
    {
        DiagnosticRubricSnapshot snapshot = rubric.Snapshot;
        if (string.IsNullOrWhiteSpace(snapshot.Id)
            || snapshot.Version < 1
            || snapshot.Revision.Length != 64
            || snapshot.BankVersion < 1
            || snapshot.BankRevision.Length != 64
            || snapshot.WilsonZ is < 1d or > 3d)
        {
            throw new InvalidDataException("L'identité du barème est invalide.");
        }

        if (snapshot.DifficultyWeights.Count != 3
            || snapshot.DifficultyWeights.Select(item => item.Difficulty).Distinct().Count() != 3
            || snapshot.DifficultyWeights.Any(item => item.Difficulty is < 1 or > 3 || item.Weight <= 0m))
        {
            throw new InvalidDataException("Les poids de difficulté du barème sont invalides.");
        }

        if (snapshot.DomainWeights.Count != DiagnosticDomains.All.Count
            || snapshot.DomainWeights.Select(item => item.Domain).Distinct().Count() != DiagnosticDomains.All.Count
            || snapshot.DomainWeights.Any(item => item.Weight <= 0m))
        {
            throw new InvalidDataException("Les poids de domaine du barème sont invalides.");
        }

        if (snapshot.CriticalGapScoreThreshold is < 0m or > 100m
            || snapshot.DevelopingLowerBound is < 0m or > 100m
            || snapshot.OperationalLowerBound <= snapshot.DevelopingLowerBound
            || snapshot.StrongLowerBound <= snapshot.OperationalLowerBound
            || snapshot.StrongLowerBound > 100m)
        {
            throw new InvalidDataException("Les seuils du barème sont invalides.");
        }
    }

    private static decimal RoundPercent(decimal value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private sealed record WeightedObservation(
        DiagnosticDomain Domain,
        decimal DifficultyWeight,
        decimal DomainWeight,
        bool Answered,
        bool Correct);
}
