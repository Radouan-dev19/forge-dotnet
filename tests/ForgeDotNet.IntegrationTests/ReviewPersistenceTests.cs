using System.Globalization;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

[Trait("Category", "ReviewScheduling")]
public sealed class ReviewPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    [Fact]
    public async Task GenerationIsIdempotentAnswerIsConcurrentAndSnapshotSurvivesMissingSource()
    {
        string dataDirectory;
        Guid profileId;
        Guid itemId;
        var clock = new FixedTimeProvider(Now);

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        {
            dataDirectory = firstRun.DataDirectory;
            profileId = (await firstRun.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
            IReviewRepository repository = firstRun.GetRequiredService<IReviewRepository>();
            ReviewItem proposed = CreateChoiceItem(profileId, "diagnostic:session:q1", "bank-v1");
            ReviewItem first = await repository.CreateOrGetAsync(proposed);
            ReviewItem replay = await repository.CreateOrGetAsync(proposed);
            itemId = first.Id;

            Assert.Equal(first.Id, replay.Id);
            Assert.Equal(first.Source, replay.Source);
            Assert.True(first.Card.Choices.SequenceEqual(replay.Card.Choices));
            Assert.Equal(first.DueOn, replay.DueOn);
            Assert.Equal(1L, await ScalarAsync(firstRun.DatabasePath, "SELECT COUNT(*) FROM ReviewItems;"));

            ReviewTransition firstTransition = ReviewRules.Answer(
                first,
                new ReviewAnswer("b", null),
                ReviewPolicyCatalog.Version1,
                Paris,
                Now);
            ReviewTransition competingTransition = ReviewRules.Answer(
                first,
                new ReviewAnswer("b", null),
                ReviewPolicyCatalog.Version1,
                Paris,
                Now);
            Exception? firstError = null;
            Exception? secondError = null;
            await Task.WhenAll(
                Task.Run(async () => firstError = await Record.ExceptionAsync(
                    () => repository.SaveTransitionAsync(profileId, 1, firstTransition).AsTask())),
                Task.Run(async () => secondError = await Record.ExceptionAsync(
                    () => repository.SaveTransitionAsync(profileId, 1, competingTransition).AsTask())));

            Assert.Equal(1, new[] { firstError, secondError }.Count(error => error is null));
            Assert.Equal(1, new[] { firstError, secondError }.Count(error => error is InvalidOperationException));
            Assert.Equal(1L, await ScalarAsync(firstRun.DatabasePath, "SELECT COUNT(*) FROM ReviewAttempts;"));
            string attemptColumns = await ColumnNamesAsync(firstRun.DatabasePath, "ReviewAttempts");
            Assert.Contains("ResponseFingerprint", attemptColumns, StringComparison.Ordinal);
            Assert.DoesNotContain(",Response,", $",{attemptColumns},", StringComparison.Ordinal);
            Assert.DoesNotContain(",Answer,", $",{attemptColumns},", StringComparison.Ordinal);

            ReviewItem stored = Assert.Single(await repository.ListActiveAsync(profileId));
            Assert.Equal(2, stored.Version);
            Assert.Equal(1, stored.AttemptCount);
            Assert.Equal(new DateOnly(2026, 8, 5), stored.DueOn);

            IMasteryEvidenceSource evidenceSource = firstRun.GetRequiredService<IMasteryEvidenceSource>();
            MasteryObservation review = Assert.Single(
                (await evidenceSource.ReadAsync(profileId)).Observations,
                observation => observation.Source == MasteryEvidenceSource.Review);
            Assert.Equal(MasteryVerificationKind.ReviewEngine, review.Verification);
            Assert.Equal(MasteryComponent.SpacedRetention, review.Component);

            ReviewItem revised = CreateChoiceItem(profileId, "diagnostic:session:q1", "bank-v2");
            Assert.NotEqual(itemId, revised.Id);
            _ = await repository.CreateOrGetAsync(revised);
            Assert.Equal(2L, await ScalarAsync(firstRun.DatabasePath, "SELECT COUNT(*) FROM ReviewItems;"));
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(dataDirectory, timeProvider: clock);
        IReviewRepository restoredRepository = secondRun.GetRequiredService<IReviewRepository>();
        ReviewItem? restored = await restoredRepository.GetAsync(profileId, itemId);
        Assert.NotNull(restored);
        Assert.Equal("Question privée figée ?", restored.Card.Question);
        Assert.Equal("b", restored.Card.ExpectedAnswer);
        Assert.Equal(2L, await ScalarAsync(secondRun.DatabasePath, "SELECT COUNT(*) FROM ReviewItems;"));
    }

    private static ReviewItem CreateChoiceItem(Guid profileId, string sourceKey, string revision)
    {
        var source = new ReviewSource(
            sourceKey,
            ReviewSourceKind.MissedDiagnosticQuestion,
            "diagnostic-q1",
            1,
            revision,
            Now.AddDays(-2));
        var card = new ReviewCard(
            "Question privée figée ?",
            "b",
            [new("a", "Première réponse"), new("b", "Seconde réponse")],
            ReviewEvaluationMode.Choice,
            CanProduceMasteryEvidence: true);
        return ReviewRules.Create(
            profileId,
            source,
            MasteryDomain.CSharp,
            ReviewScheduleKind.Recovery,
            card,
            ReviewPolicyCatalog.Version1,
            Paris,
            Now.AddDays(-2));
    }

    private static async Task<long> ScalarAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<string> ColumnNamesAsync(string databasePath, string table)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT group_concat(name, ',') FROM pragma_table_info('{table}');";
        return Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
