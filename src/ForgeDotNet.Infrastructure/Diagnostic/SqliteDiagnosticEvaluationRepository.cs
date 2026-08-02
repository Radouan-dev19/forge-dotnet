using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Diagnostic;

public sealed class SqliteDiagnosticEvaluationRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IDiagnosticEvaluationRepository
{
    private const int MaximumRubricJsonLength = 16_384;
    private const int MaximumReportJsonLength = 65_536;
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public async ValueTask<DiagnosticEvaluationData?> GetAsync(
        Guid profileId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, sessionId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        bool sessionExists = await context.DiagnosticSessions
            .AsNoTracking()
            .AnyAsync(
                session => session.Id == sessionId && session.ProfileId == profileId,
                cancellationToken);
        if (!sessionExists)
        {
            return null;
        }

        DiagnosticEvaluationRecord? record = await context.DiagnosticEvaluations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);
        return record is null ? null : ToData(profileId, record);
    }

    public async ValueTask<DiagnosticEvaluationData> CreateOrGetAsync(
        DiagnosticEvaluationData evaluation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ValidateKeys(evaluation.ProfileId, evaluation.SessionId);
        string rubricJson = Serialize(evaluation.Report.Rubric, MaximumRubricJsonLength, "barème figé");
        string reportJson = Serialize(evaluation.Report, MaximumReportJsonLength, "rapport d'évaluation");
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        bool sessionExists = await context.DiagnosticSessions.AnyAsync(
            session => session.Id == evaluation.SessionId && session.ProfileId == evaluation.ProfileId,
            cancellationToken);
        if (!sessionExists)
        {
            throw new KeyNotFoundException("La session de diagnostic n'existe pas.");
        }

        DiagnosticEvaluationRecord? existing = await context.DiagnosticEvaluations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.SessionId == evaluation.SessionId, cancellationToken);
        if (existing is not null)
        {
            return ToData(evaluation.ProfileId, existing);
        }

        context.DiagnosticEvaluations.Add(new DiagnosticEvaluationRecord
        {
            SessionId = evaluation.SessionId,
            RubricId = evaluation.Report.Rubric.Id,
            RubricVersion = evaluation.Report.Rubric.Version,
            RubricRevision = evaluation.Report.Rubric.Revision,
            FrozenRubricJson = rubricJson,
            ReportJson = reportJson,
            CreatedAtUtc = evaluation.CreatedAtUtc,
        });
        await context.SaveChangesAsync(cancellationToken);
        return evaluation;
    }

    private static DiagnosticEvaluationData ToData(Guid profileId, DiagnosticEvaluationRecord record)
    {
        DiagnosticRubricSnapshot rubric = JsonSerializer.Deserialize<DiagnosticRubricSnapshot>(
            record.FrozenRubricJson,
            SerializerOptions)
            ?? throw new InvalidDataException("Le barème figé est illisible.");
        DiagnosticEvaluationReport report = JsonSerializer.Deserialize<DiagnosticEvaluationReport>(
            record.ReportJson,
            SerializerOptions)
            ?? throw new InvalidDataException("Le rapport d'évaluation est illisible.");
        if (!RubricsEqual(rubric, report.Rubric)
            || !string.Equals(record.RubricId, rubric.Id, StringComparison.Ordinal)
            || record.RubricVersion != rubric.Version
            || !string.Equals(record.RubricRevision, rubric.Revision, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Le rapport ne correspond pas au barème figé.");
        }

        return new DiagnosticEvaluationData(record.SessionId, profileId, report, record.CreatedAtUtc);
    }

    private static bool RubricsEqual(DiagnosticRubricSnapshot left, DiagnosticRubricSnapshot right) =>
        string.Equals(left.Id, right.Id, StringComparison.Ordinal)
        && left.Version == right.Version
        && string.Equals(left.Revision, right.Revision, StringComparison.Ordinal)
        && string.Equals(left.BankId, right.BankId, StringComparison.Ordinal)
        && left.BankVersion == right.BankVersion
        && string.Equals(left.BankRevision, right.BankRevision, StringComparison.Ordinal)
        && left.DifficultyWeights.SequenceEqual(right.DifficultyWeights)
        && left.DomainWeights.SequenceEqual(right.DomainWeights)
        && left.CriticalGapScoreThreshold == right.CriticalGapScoreThreshold
        && left.DevelopingLowerBound == right.DevelopingLowerBound
        && left.OperationalLowerBound == right.OperationalLowerBound
        && left.StrongLowerBound == right.StrongLowerBound
        && left.WilsonZ.Equals(right.WilsonZ);

    private static string Serialize<T>(T value, int maximumLength, string context)
    {
        string json = JsonSerializer.Serialize(value, SerializerOptions);
        if (json.Length > maximumLength)
        {
            throw new InvalidDataException($"Le {context} dépasse la taille autorisée.");
        }

        return json;
    }

    private static void ValidateKeys(Guid profileId, Guid sessionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
