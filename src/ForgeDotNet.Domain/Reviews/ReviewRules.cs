using System.Security.Cryptography;
using System.Text;

namespace ForgeDotNet.Domain.Reviews;

public static class ReviewRules
{
    private static readonly int[] ExpectedGeneralIntervals = [1, 3, 7, 14, 30];
    private static readonly int[] ExpectedRecoveryIntervals = [1, 7, 14, 30];

    public static ReviewItem Create(
        Guid profileId,
        ReviewSource source,
        ForgeDotNet.Domain.Mastery.MasteryDomain domain,
        ReviewScheduleKind scheduleKind,
        ReviewCard card,
        ReviewPolicy policy,
        TimeZoneInfo timeZone,
        DateTimeOffset createdAtUtc)
    {
        ValidatePolicy(policy, timeZone);
        ValidateSource(source, createdAtUtc);
        ValidateCard(card);
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("Le profil de révision est obligatoire.", nameof(profileId));
        }

        if (!Enum.IsDefined(domain) || !Enum.IsDefined(scheduleKind))
        {
            throw new ArgumentException("Le domaine ou le type de calendrier est invalide.");
        }

        IReadOnlyList<int> intervals = Intervals(policy, scheduleKind);
        DateOnly sourceDate = LocalDate(source.OccurredAtUtc, timeZone);
        return new ReviewItem(
            DeterministicId(profileId, source, policy),
            profileId,
            source,
            domain,
            scheduleKind,
            card,
            policy.Id,
            policy.Version,
            policy.Revision,
            0,
            sourceDate.AddDays(intervals[0]),
            0,
            1,
            true,
            createdAtUtc,
            null);
    }

    public static ReviewTransition Answer(
        ReviewItem item,
        ReviewAnswer answer,
        ReviewPolicy policy,
        TimeZoneInfo timeZone,
        DateTimeOffset answeredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(answer);
        ValidatePolicy(policy, timeZone);
        ValidateItem(item, policy, answeredAtUtc);
        if (answeredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La date de réponse doit être en UTC.", nameof(answeredAtUtc));
        }

        DateOnly answeredOn = LocalDate(answeredAtUtc, timeZone);
        if (answeredOn < item.DueOn)
        {
            throw new InvalidOperationException("Une carte ne peut pas être validée avant son échéance.");
        }

        (ReviewOutcome outcome, bool verified) = Evaluate(item.Card, answer);
        IReadOnlyList<int> intervals = Intervals(policy, item.ScheduleKind);
        int nextIndex = outcome == ReviewOutcome.Succeeded
            ? Math.Min(item.CurrentIntervalIndex + 1, intervals.Count - 1)
            : 0;
        int nextInterval = intervals[nextIndex];
        DateOnly nextDue = answeredOn.AddDays(nextInterval);
        bool masteryEligible = verified
            && item.Card.CanProduceMasteryEvidence
            && item.Source.Kind == ReviewSourceKind.MissedDiagnosticQuestion
            && item.Card.EvaluationMode == ReviewEvaluationMode.Choice;
        decimal score = outcome == ReviewOutcome.Succeeded ? 100m : 0m;
        string fingerprint = Fingerprint(answer);
        var attempt = new ReviewAttempt(
            Guid.NewGuid(),
            item.Id,
            item.AttemptCount + 1,
            outcome,
            verified,
            masteryEligible,
            score,
            fingerprint,
            item.DueOn,
            nextDue,
            nextInterval,
            answeredAtUtc);
        ReviewItem updated = item with
        {
            CurrentIntervalIndex = nextIndex,
            DueOn = nextDue,
            AttemptCount = item.AttemptCount + 1,
            Version = item.Version + 1,
            LastReviewedAtUtc = answeredAtUtc,
        };
        return new ReviewTransition(updated, attempt);
    }

    public static DateOnly LocalDate(DateTimeOffset instant, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
    }

    public static int CurrentIntervalDays(ReviewItem item, ReviewPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(item);
        IReadOnlyList<int> intervals = Intervals(policy, item.ScheduleKind);
        if (item.CurrentIntervalIndex < 0 || item.CurrentIntervalIndex >= intervals.Count)
        {
            throw new InvalidDataException("L’étape de révision est invalide.");
        }

        return intervals[item.CurrentIntervalIndex];
    }

    private static (ReviewOutcome Outcome, bool Verified) Evaluate(ReviewCard card, ReviewAnswer answer)
    {
        string response = Normalize(answer.Response, 4_000, "La réponse");
        return card.EvaluationMode switch
        {
            ReviewEvaluationMode.SelfAssessment when answer.SelfReportedSuccess is not null =>
                (answer.SelfReportedSuccess.Value ? ReviewOutcome.Succeeded : ReviewOutcome.Failed, false),
            ReviewEvaluationMode.SelfAssessment =>
                throw new ArgumentException("L’autoévaluation explicite est obligatoire.", nameof(answer)),
            ReviewEvaluationMode.Choice when answer.SelfReportedSuccess is null =>
                (string.Equals(response, card.ExpectedAnswer, StringComparison.Ordinal)
                    ? ReviewOutcome.Succeeded
                    : ReviewOutcome.Failed, true),
            ReviewEvaluationMode.ExactText when answer.SelfReportedSuccess is null =>
                (string.Equals(NormalizeForComparison(response), NormalizeForComparison(card.ExpectedAnswer!), StringComparison.Ordinal)
                    ? ReviewOutcome.Succeeded
                    : ReviewOutcome.Failed, true),
            _ => throw new ArgumentException("La réponse ne correspond pas au mode d’évaluation de la carte.", nameof(answer)),
        };
    }

    private static string Fingerprint(ReviewAnswer answer)
    {
        string value = $"{NormalizeForComparison(answer.Response)}\n{answer.SelfReportedSuccess?.ToString() ?? "server"}";
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))}";
    }

    private static string NormalizeForComparison(string value) => string.Join(
        ' ',
        value.Normalize(NormalizationForm.FormKC)
            .Trim()
            .ToUpperInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static Guid DeterministicId(Guid profileId, ReviewSource source, ReviewPolicy policy)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{profileId:N}|{source.Key}|{source.Revision}|{policy.Revision}"));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static IReadOnlyList<int> Intervals(ReviewPolicy policy, ReviewScheduleKind kind) => kind switch
    {
        ReviewScheduleKind.General => policy.GeneralIntervalsDays,
        ReviewScheduleKind.Recovery => policy.RecoveryIntervalsDays,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static void ValidateItem(ReviewItem item, ReviewPolicy policy, DateTimeOffset answeredAtUtc)
    {
        if (item.Id == Guid.Empty
            || item.ProfileId == Guid.Empty
            || !item.IsActive
            || item.Version < 1
            || item.AttemptCount < 0
            || item.AttemptCount != item.Version - 1
            || !string.Equals(item.PolicyId, policy.Id, StringComparison.Ordinal)
            || item.PolicyVersion != policy.Version
            || !string.Equals(item.PolicyRevision, policy.Revision, StringComparison.Ordinal)
            || item.CreatedAtUtc > answeredAtUtc
            || item.LastReviewedAtUtc > answeredAtUtc)
        {
            throw new InvalidDataException("La carte de révision est incohérente.");
        }

        ValidateSource(item.Source, answeredAtUtc);
        ValidateCard(item.Card);
        _ = CurrentIntervalDays(item, policy);
    }

    private static void ValidateSource(ReviewSource source, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.Key)
            || source.Key.Length > 200
            || !Enum.IsDefined(source.Kind)
            || string.IsNullOrWhiteSpace(source.ItemId)
            || source.ItemId.Length > 160
            || source.ItemVersion < 1
            || string.IsNullOrWhiteSpace(source.Revision)
            || source.Revision.Length > 80
            || source.OccurredAtUtc.Offset != TimeSpan.Zero
            || source.OccurredAtUtc > nowUtc.AddMinutes(5))
        {
            throw new InvalidDataException("La source de révision est invalide.");
        }
    }

    private static void ValidateCard(ReviewCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        _ = Normalize(card.Question, 2_000, "La question");
        if (!Enum.IsDefined(card.EvaluationMode)
            || card.Choices.Count > 8
            || card.Choices.Any(choice =>
                string.IsNullOrWhiteSpace(choice.Id)
                || choice.Id.Length > 40
                || string.IsNullOrWhiteSpace(choice.Text)
                || choice.Text.Length > 600)
            || card.Choices.Select(choice => choice.Id).Distinct(StringComparer.Ordinal).Count() != card.Choices.Count)
        {
            throw new InvalidDataException("La carte de révision est invalide.");
        }

        if (card.EvaluationMode == ReviewEvaluationMode.Choice
            && (card.Choices.Count < 2
                || card.ExpectedAnswer is null
                || !card.Choices.Any(choice => string.Equals(choice.Id, card.ExpectedAnswer, StringComparison.Ordinal))))
        {
            throw new InvalidDataException("La carte à choix ne possède pas de réponse privée valide.");
        }

        if (card.EvaluationMode == ReviewEvaluationMode.ExactText && string.IsNullOrWhiteSpace(card.ExpectedAnswer))
        {
            throw new InvalidDataException("La carte textuelle ne possède pas de réponse privée.");
        }

        if (card.ExpectedAnswer is not null)
        {
            _ = Normalize(card.ExpectedAnswer, 2_000, "La réponse attendue");
        }

        if (card.CanProduceMasteryEvidence && card.EvaluationMode != ReviewEvaluationMode.Choice)
        {
            throw new InvalidDataException("Seule une carte à choix vérifiée peut produire une preuve de maîtrise.");
        }
    }

    private static void ValidatePolicy(ReviewPolicy policy, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(timeZone);
        if (string.IsNullOrWhiteSpace(policy.Id)
            || policy.Version < 1
            || string.IsNullOrWhiteSpace(policy.Revision)
            || !string.Equals(policy.TimeZoneId, timeZone.Id, StringComparison.OrdinalIgnoreCase)
            || !policy.GeneralIntervalsDays.SequenceEqual(ExpectedGeneralIntervals)
            || !policy.RecoveryIntervalsDays.SequenceEqual(ExpectedRecoveryIntervals))
        {
            throw new InvalidDataException("La politique de révision est invalide.");
        }
    }

    private static string Normalize(string value, int maximumLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength
            || value.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t'))
        {
            throw new ArgumentException($"{label} est vide, trop longue ou contient un caractère interdit.");
        }

        return value.Trim();
    }
}
