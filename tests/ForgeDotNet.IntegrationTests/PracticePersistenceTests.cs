using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Practice;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Practice;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class PracticePersistenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ManualProtocolIsProtectedVersionedAndSurvivesRestart()
    {
        string dataDirectory;
        var clock = new FixedTimeProvider(Start);

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        using (PracticeSourceFixture source = await PracticeSourceFixture.CreateAsync())
        using (var coordinator = new PracticeCoordinator())
        {
            dataDirectory = firstRun.DataDirectory;
            PracticeService service = CreateService(firstRun, source.Source, coordinator, clock);
            PracticeActivityView initial = await service.GetOrStartAsync("reference-total-001");

            Assert.Equal(PracticeActivityState.ReflectionRequired, initial.State);
            Assert.Null(initial.Solution);
            Assert.Null(initial.VariantStatement);
            Assert.Empty(initial.UsedHints);
            Assert.False(initial.SolutionEligibility.CanViewSolution);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ViewSolutionAsync(initial.ExerciseId, initial.Version).AsTask());

            PracticeActivityView reflected = await service.SaveReflectionAsync(
                initial.ExerciseId,
                initial.Version,
                CompleteReflection());
            Assert.Equal(PracticeActivityState.Attempting, reflected.State);

            Exception?[] hintResults = await RunConcurrentlyAsync(
                () => service.UnlockHintAsync(reflected.ExerciseId, reflected.Version, requestedLevel: 1).AsTask(),
                () => service.UnlockHintAsync(reflected.ExerciseId, reflected.Version, requestedLevel: 1).AsTask());
            Assert.Single(hintResults, exception => exception is null);
            Assert.Single(hintResults, exception => exception is InvalidOperationException);
            PracticeActivityView afterHint = await service.GetOrStartAsync(reflected.ExerciseId);
            PracticeHintUsageView firstHint = Assert.Single(afterHint.UsedHints);
            Assert.Equal(1, firstHint.Level);
            Assert.Contains("Quel invariant de", firstHint.Content, StringComparison.Ordinal);
            Assert.DoesNotContain("Concentrez la", firstHint.Content, StringComparison.Ordinal);

            string firstSubmission = LongAttempt(
                "Je parcours la liste avec une boucle foreach et j'ajoute chaque montant à un accumulateur decimal initialisé à zéro");
            Exception?[] attemptResults = await RunConcurrentlyAsync(
                () => service.SubmitAttemptAsync(
                    afterHint.ExerciseId,
                    afterHint.Version,
                    SeriousAttempt(firstSubmission)).AsTask(),
                () => service.SubmitAttemptAsync(
                    afterHint.ExerciseId,
                    afterHint.Version,
                    SeriousAttempt(firstSubmission)).AsTask());
            Assert.Single(attemptResults, exception => exception is null);
            Assert.Single(attemptResults, exception => exception is InvalidOperationException);

            PracticeActivityView afterFirst = await service.GetOrStartAsync(afterHint.ExerciseId);
            Assert.Single(afterFirst.Attempts);
            Assert.True(afterFirst.Attempts[0].IsSerious);
            clock.Advance(TimeSpan.FromMinutes(1));
            PracticeActivityView afterSecond = await service.SubmitAttemptAsync(
                afterFirst.ExerciseId,
                afterFirst.Version,
                SeriousAttempt(LongAttempt(
                    "Je construis le même résultat avec une agrégation explicite qui conserve decimal et traite la collection vide comme zéro")));
            Assert.Equal(2, afterSecond.SolutionEligibility.SeriousAttemptCount);
            Assert.False(afterSecond.SolutionEligibility.CanViewSolution);

            clock.Advance(TimeSpan.FromMinutes(8));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ViewSolutionAsync(afterSecond.ExerciseId, afterSecond.Version).AsTask());
            clock.Advance(TimeSpan.FromMinutes(2));
            PracticeActivityView solutionViewed = await service.ViewSolutionAsync(
                afterSecond.ExerciseId,
                afterSecond.Version);
            Assert.Equal(PracticeActivityState.SolutionViewed, solutionViewed.State);
            Assert.NotNull(solutionViewed.Solution);
            Assert.Contains("return first + second;", solutionViewed.Solution, StringComparison.Ordinal);
            Assert.DoesNotContain("tests/hidden", solutionViewed.Solution, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("non maîtrisée", solutionViewed.StateLabel, StringComparison.OrdinalIgnoreCase);

            PracticeActivityView completed = await service.CompletePostSolutionWorkAsync(
                solutionViewed.ExerciseId,
                solutionViewed.Version,
                LongAttempt("J'explique que decimal garde la représentation métier et que la collection vide est couverte par l'initialisation du cumul"),
                LongAttempt("Pour la variante je valide la remise aux deux bornes puis je calcule le brut avant d'appliquer le coefficient"));
            Assert.Equal(PracticeActivityState.PostSolutionCompleted, completed.State);
            Assert.Contains("non maîtrisée", completed.StateLabel, StringComparison.OrdinalIgnoreCase);

            await using var connection = new SqliteConnection($"Data Source={firstRun.DatabasePath};Mode=ReadOnly");
            await connection.OpenAsync();
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM PracticeActivities;"));
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM PracticeReflections;"));
            Assert.Equal(2L, await ScalarAsync(connection, "SELECT COUNT(*) FROM PracticeAttempts;"));
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM PracticeHintUsages;"));
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(
            dataDirectory,
            timeProvider: clock);
        using PracticeSourceFixture restoredSource = await PracticeSourceFixture.CreateAsync();
        using var restoredCoordinator = new PracticeCoordinator();
        PracticeService restoredService = CreateService(
            secondRun,
            restoredSource.Source,
            restoredCoordinator,
            clock);

        PracticeActivityView restored = await restoredService.GetOrStartAsync("reference-total-001");

        Assert.Equal(PracticeActivityState.PostSolutionCompleted, restored.State);
        Assert.Equal(2, restored.Attempts.Count);
        Assert.Single(restored.UsedHints);
        Assert.NotNull(restored.PersonalExplanation);
        Assert.NotNull(restored.VariantSubmission);
    }

    [Fact]
    public async Task PrivateSourceNeverProjectsUnusedHintsOrSolutionThroughInitialView()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        using PracticeSourceFixture source = await PracticeSourceFixture.CreateAsync();
        using var coordinator = new PracticeCoordinator();
        PracticeService service = CreateService(environment, source.Source, coordinator, TimeProvider.System);

        PracticeActivityView view = await service.GetOrStartAsync("reference-total-001");
        string serialized = System.Text.Json.JsonSerializer.Serialize(view);

        Assert.Null(view.Solution);
        Assert.Null(view.Explanation);
        Assert.Null(view.VariantStatement);
        Assert.Empty(view.UsedHints);
        Assert.DoesNotContain("accumulateur décimal initialisé", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tests/hidden", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution/", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static PracticeService CreateService(
        PersistenceTestEnvironment environment,
        IPracticeExerciseSource source,
        PracticeCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IPracticeActivityRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            clock,
            environment.GetRequiredService<IExamAccessPolicy>());

    private static PracticeReflectionInput CompleteReflection() => new(
        "Je dois additionner chaque montant decimal en conservant les avoirs et la précision attendue.",
        "Une liste en lecture seule de montants decimal qui peut être vide.",
        "Le total decimal de tous les éléments, avec zéro pour une liste vide.",
        "Liste vide, valeurs négatives, petits montants et valeurs élevées sans conversion en double.",
        "Un accumulateur decimal mis à jour une fois par élément respecte les cas attendus.",
        "Initialiser un cumul à zéro, parcourir chaque montant, l'ajouter puis retourner le résultat final.");

    private static PracticeAttemptInput SeriousAttempt(string submission) => new(
        submission,
        "J'ai vérifié manuellement le cas vide, le cas nominal et un avoir ; aucun résultat automatique Forge.NET n'est impliqué.",
        ManualCheckDeclared: true);

    private static string LongAttempt(string text) =>
        $"{text}. La proposition précise aussi le résultat attendu et les limites relues manuellement pour documenter une évolution réelle.";

    private static async Task<Exception?[]> RunConcurrentlyAsync(
        Func<Task<PracticeActivityView>> first,
        Func<Task<PracticeActivityView>> second) => await Task.WhenAll(
            CaptureAsync(first),
            CaptureAsync(second));

    private static async Task<Exception?> CaptureAsync(Func<Task<PracticeActivityView>> operation)
    {
        try
        {
            _ = await operation();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private sealed class PracticeSourceFixture(
        ContentCatalogProvider provider,
        FileSystemPracticeExerciseSource source) : IDisposable
    {
        public IPracticeExerciseSource Source => source;

        public static async Task<PracticeSourceFixture> CreateAsync()
        {
            string contentRoot = FindContentRoot();
            string catalogDirectory = Path.Combine(contentRoot, "reference");
            var options = new ContentValidationOptions { ContentRootPath = contentRoot };
            var validation = new FileSystemContentValidationService(options);
            var loader = new FileSystemContentCatalogLoader(validation, options);
            var provider = new ContentCatalogProvider(loader);
            ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogDirectory);
            Assert.True(reload.Succeeded);
            var source = new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
            {
                ContentRootPath = contentRoot,
                CatalogDirectoryPath = catalogDirectory,
            });
            return new PracticeSourceFixture(provider, source);
        }

        public void Dispose() => provider.Dispose();
    }

    private static string FindContentRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "schemas", "exercise.schema.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("La racine de contenu de référence est introuvable.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
