namespace ForgeDotNet.Domain.Mastery;

public enum MasteryDomain
{
    CSharp,
    Debugging,
    Sql,
    Api,
    Tests,
    Docker,
    ContinuousIntegration,
    Security,
    Architecture,
    Performance,
    English,
}

public enum MasteryComponent
{
    AutonomousPractice,
    UnassistedExam,
    SpacedRetention,
    Explanation,
    Quiz,
}

public enum MasteryAssistance
{
    None,
    Hint1,
    Hint2,
    Hint3,
    Hint4,
    Solution,
}

public enum MasteryEvidenceSource
{
    Practice,
    DebugLab,
    SqlLab,
    Exam,
    Review,
    Explanation,
    Quiz,
    Deliverable,
}

public enum MasteryVerificationKind
{
    ManualDeclaration,
    AutomaticTests,
    ServerRubric,
    ExamEngine,
    ReviewEngine,
    QuizEngine,
}

public enum MasteryGate
{
    A,
    B,
    C,
    D,
}

public enum MasteryGateRequirementKind
{
    PreviousGate,
    DomainScore,
    UnassistedExerciseCount,
    Achievement,
}

public sealed record MasteryObservation(
    Guid Id,
    Guid ProfileId,
    MasteryDomain Domain,
    MasteryComponent Component,
    MasteryEvidenceSource Source,
    MasteryVerificationKind Verification,
    string ItemId,
    int ItemVersion,
    string ContentRevision,
    decimal Score,
    MasteryAssistance Assistance,
    DateTimeOffset ObservedAtUtc,
    string EvidenceReference);

public sealed record MasteryAchievement(
    Guid Id,
    Guid ProfileId,
    string Key,
    MasteryVerificationKind Verification,
    bool Passed,
    int DurationMinutes,
    DateTimeOffset ObservedAtUtc,
    string EvidenceReference);

public sealed record MasteryComponentPolicy(MasteryComponent Component, decimal Weight);

public sealed record MasteryGateRequirement(
    MasteryGateRequirementKind Kind,
    string Label,
    MasteryGate? PreviousGate = null,
    MasteryDomain? Domain = null,
    decimal MinimumScore = 0,
    int MinimumCount = 0,
    string? AchievementKey = null,
    int MinimumDurationMinutes = 0);

public sealed record MasteryGatePolicy(
    MasteryGate Gate,
    string Label,
    IReadOnlyList<MasteryGateRequirement> Requirements);

public sealed record MasteryPolicy(
    string Id,
    int Version,
    string Revision,
    IReadOnlyList<MasteryComponentPolicy> Components,
    decimal ModuleThreshold,
    decimal CriticalModuleThreshold,
    IReadOnlyList<MasteryDomain> CriticalDomains,
    int MinimumDistinctItems,
    int RecentProofDays,
    int MaximumEvidenceAgeDays,
    IReadOnlyList<MasteryGatePolicy> Gates);

public sealed record MasteryComponentScore(
    MasteryComponent Component,
    decimal Weight,
    decimal Score,
    bool HasEvidence,
    int EvidenceCount,
    int DistinctItemCount);

public sealed record MasteryDomainScore(
    MasteryDomain Domain,
    decimal Score,
    decimal RequiredScore,
    bool IsCritical,
    bool IsValidated,
    bool HasRecentUnassistedEvidence,
    bool HasUnassistedExam,
    int DistinctItemCount,
    IReadOnlyList<MasteryComponentScore> Components,
    IReadOnlyList<string> Blockers);

public sealed record MasteryGateResult(
    MasteryGate Gate,
    string Label,
    bool IsOpen,
    IReadOnlyList<string> Blockers);

public sealed record MasterySnapshot(
    Guid ProfileId,
    string PolicyId,
    int PolicyVersion,
    string PolicyRevision,
    string EvidenceRevision,
    DateTimeOffset CalculatedAtUtc,
    int ObservationCount,
    IReadOnlyList<MasteryDomainScore> Domains,
    IReadOnlyList<MasteryGateResult> Gates);

public sealed record MasteryEvidenceSet(
    IReadOnlyList<MasteryObservation> Observations,
    IReadOnlyList<MasteryAchievement> Achievements,
    string Revision);
