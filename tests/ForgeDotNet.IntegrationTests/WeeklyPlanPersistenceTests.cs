using System.Text;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.WeeklyPlanning;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.WeeklyPlanning;
using ForgeDotNet.Infrastructure.Diagnostic;
using ForgeDotNet.Infrastructure.WeeklyPlanning;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class WeeklyPlanPersistenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 26, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DiagnosticToAdjustedAcceptedPlanIsVersionedAndSurvivesRestart()
    {
        string dataDirectory;
        Guid sessionId;
        DateTimeOffset acceptedAt;
        var clock = new FixedTimeProvider(Start);

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        {
            dataDirectory = firstRun.DataDirectory;
            ILocalProfileRepository profiles = firstRun.GetRequiredService<ILocalProfileRepository>();
            UserProfile profile = await profiles.GetAsync();
            await profiles.SaveAsync(profile.Update(
                "Profil test",
                "Consolider les fondamentaux .NET",
                12,
                InterfaceLanguage.French));

            using var diagnosticSource = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
            using var diagnosticCoordinator = new DiagnosticSessionCoordinator();
            DiagnosticSessionService sessions = CreateSessionService(
                firstRun,
                diagnosticSource,
                diagnosticCoordinator,
                clock);
            DiagnosticSessionView completed = await CompleteReducedAsync(sessions, diagnosticSource);
            sessionId = completed.Id;
            DiagnosticEvaluationService evaluations = CreateEvaluationService(
                firstRun,
                diagnosticSource,
                diagnosticCoordinator,
                clock);
            _ = await evaluations.GetOrCreateAsync(sessionId);

            using var curriculumSource = CreateCurriculumSource();
            using var planCoordinator = new WeeklyPlanCoordinator();
            WeeklyPlanService plans = CreatePlanService(
                firstRun,
                curriculumSource,
                planCoordinator,
                clock);
            WeeklyPlanView initial = await plans.GetOrCreateAsync(sessionId);

            Assert.Equal(1, initial.Version);
            Assert.Equal(WeeklyPlanStatus.Draft, initial.Status);
            Assert.Equal(12, initial.TargetWeeklyHours);
            Assert.Equal(24, initial.Weeks.Count);
            Assert.True(initial.IsProvisional);
            Assert.All(initial.Weeks, week => Assert.True(week.KnowledgeCheckRequired));

            UserProfile updatedProfile = (await profiles.GetAsync()).Update(
                "Profil test",
                "Consolider les fondamentaux .NET",
                14,
                InterfaceLanguage.French);
            await profiles.SaveAsync(updatedProfile);
            WeeklyPlanView refreshed = await plans.GetOrCreateAsync(sessionId);
            Assert.Equal(12, refreshed.ProfileAvailableHours);
            Assert.Equal(14, refreshed.CurrentProfileAvailableHours);

            WeeklyPlanView adjusted = await plans.AdjustHoursAsync(sessionId, initial.Version, 14);
            Assert.Equal(2, adjusted.Version);
            Assert.Equal(14, adjusted.ProfileAvailableHours);
            Assert.Equal(14, adjusted.CurrentProfileAvailableHours);
            Assert.Equal(14, adjusted.TargetWeeklyHours);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                plans.AcceptAsync(sessionId, expectedVersion: 1).AsTask());

            clock.Advance(TimeSpan.FromMinutes(5));
            WeeklyPlanView accepted = await plans.AcceptAsync(sessionId, adjusted.Version);
            WeeklyPlanView acceptedAgain = await plans.AcceptAsync(sessionId, adjusted.Version);
            Assert.Equal(WeeklyPlanStatus.Accepted, accepted.Status);
            Assert.False(accepted.CanAdjust);
            Assert.Equal(accepted.AcceptedAtUtc, acceptedAgain.AcceptedAtUtc);
            acceptedAt = accepted.AcceptedAtUtc!.Value;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                plans.AdjustHoursAsync(sessionId, accepted.Version, 9).AsTask());

            await using var connection = new SqliteConnection($"Data Source={firstRun.DatabasePath};Mode=ReadOnly");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT Version, Status, PlanJson FROM WeeklyPlans WHERE DiagnosticSessionId = $session ORDER BY Version;";
            command.Parameters.AddWithValue("$session", sessionId);
            await using SqliteDataReader reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(1L, reader.GetInt64(0));
            Assert.Equal("Draft", reader.GetString(1));
            string firstJson = reader.GetString(2);
            Assert.DoesNotContain("Profil test", firstJson, StringComparison.Ordinal);
            Assert.DoesNotContain("expectedOption", firstJson, StringComparison.OrdinalIgnoreCase);
            Assert.True(await reader.ReadAsync());
            Assert.Equal(2L, reader.GetInt64(0));
            Assert.Equal("Accepted", reader.GetString(1));
            Assert.False(await reader.ReadAsync());
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(
            dataDirectory,
            timeProvider: clock);
        using var secondCoordinator = new WeeklyPlanCoordinator();
        WeeklyPlanService restoredService = CreatePlanService(
            secondRun,
            new ThrowingCurriculumSource(),
            secondCoordinator,
            clock);

        WeeklyPlanView restored = await restoredService.GetOrCreateAsync(sessionId);

        Assert.Equal(2, restored.Version);
        Assert.Equal(WeeklyPlanStatus.Accepted, restored.Status);
        Assert.Equal(14, restored.TargetWeeklyHours);
        Assert.Equal(acceptedAt, restored.AcceptedAtUtc);
    }

    [Fact]
    public async Task CurriculumSourceRejectsForwardPrerequisite()
    {
        string root = Path.Combine(Path.GetTempPath(), "ForgeDotNet.WeeklyPlan", Guid.NewGuid().ToString("N"));
        string directory = Path.Combine(root, "planning", "v1");
        Directory.CreateDirectory(directory);
        try
        {
            string sourcePath = FindCurriculumPath();
            string json = await File.ReadAllTextAsync(sourcePath, Encoding.UTF8);
            json = json.Replace(
                "\"prerequisites\": [\"week-01\"]",
                "\"prerequisites\": [\"week-24\"]",
                StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(directory, "curriculum.json"), json, new UTF8Encoding(false));
            using var source = new FileSystemWeeklyPlanCurriculumSource(new WeeklyPlanCurriculumOptions
            {
                ContentRootPath = root,
                DirectoryPath = directory,
            });

            InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                source.GetAsync().AsTask());

            Assert.Contains("cyclique", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static async Task<DiagnosticSessionView> CompleteReducedAsync(
        DiagnosticSessionService sessions,
        FileSystemDiagnosticBankSource source)
    {
        DiagnosticScoringRubric rubric = await source.GetRubricAsync();
        DiagnosticSessionView session = await sessions.StartAsync(DiagnosticMode.Reduced);
        for (int sectionIndex = 0; sectionIndex < session.Sections.Count; sectionIndex++)
        {
            session = await sessions.GetAsync(session.Id);
            foreach (DiagnosticQuestionView question in session.CurrentSection!.Questions)
            {
                session = await sessions.SaveResponseAsync(
                    session.Id,
                    question.Id,
                    rubric.ExpectedOptions[question.Id]);
            }

            session = await sessions.CompleteSectionAsync(session.Id, sectionIndex);
            if (sectionIndex < session.Sections.Count - 1)
            {
                session = await sessions.StartCurrentSectionAsync(session.Id);
            }
        }

        return await sessions.FinishAsync(session.Id);
    }

    private static DiagnosticSessionService CreateSessionService(
        PersistenceTestEnvironment environment,
        FileSystemDiagnosticBankSource source,
        DiagnosticSessionCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IDiagnosticSessionRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            new DiagnosticSessionOptions(),
            clock);

    private static DiagnosticEvaluationService CreateEvaluationService(
        PersistenceTestEnvironment environment,
        IDiagnosticRubricSource source,
        DiagnosticSessionCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IDiagnosticSessionRepository>(),
            environment.GetRequiredService<IDiagnosticEvaluationRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            clock);

    private static WeeklyPlanService CreatePlanService(
        PersistenceTestEnvironment environment,
        IWeeklyPlanCurriculumSource source,
        WeeklyPlanCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IWeeklyPlanRepository>(),
            environment.GetRequiredService<IDiagnosticEvaluationRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            clock);

    private static FileSystemWeeklyPlanCurriculumSource CreateCurriculumSource()
    {
        string path = FindCurriculumPath();
        return new FileSystemWeeklyPlanCurriculumSource(new WeeklyPlanCurriculumOptions
        {
            ContentRootPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(path)!.FullName)!.FullName)!.FullName,
            DirectoryPath = Path.GetDirectoryName(path)!,
        });
    }

    private static string FindCurriculumPath()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "content", "planning", "v1", "curriculum.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Le curriculum de planification de référence est introuvable.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }

    private sealed class ThrowingCurriculumSource : IWeeklyPlanCurriculumSource
    {
        public ValueTask<WeeklyPlanCurriculumSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Un plan persisté ne doit pas recharger le curriculum courant.");
    }
}
