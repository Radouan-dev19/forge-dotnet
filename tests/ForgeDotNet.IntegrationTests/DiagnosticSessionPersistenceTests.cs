using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Infrastructure.Diagnostic;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class DiagnosticSessionPersistenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FrozenSessionAndAutosavedResponseSurviveCompleteRestart()
    {
        string dataDirectory;
        Guid sessionId;
        string[] questionIds;
        var clock = new AdjustableTimeProvider(Start);

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        {
            dataDirectory = firstRun.DataDirectory;
            using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
            using var coordinator = new DiagnosticSessionCoordinator();
            DiagnosticSessionService service = CreateService(firstRun, source, coordinator, clock);
            DiagnosticSessionView started = await service.StartAsync(DiagnosticMode.Reduced);
            DiagnosticSessionView duplicateStart = await service.StartAsync(DiagnosticMode.Initial);
            sessionId = started.Id;
            var profile = await firstRun.GetRequiredService<ILocalProfileRepository>().GetAsync();
            DiagnosticSessionData stored = await firstRun
                .GetRequiredService<IDiagnosticSessionRepository>()
                .GetAsync(profile.LocalId, sessionId)
                ?? throw new InvalidOperationException("Session de test absente.");
            questionIds = stored.Plan.Sections
                .SelectMany(section => section.Questions)
                .Select(question => question.Id)
                .ToArray();
            DiagnosticQuestionView firstQuestion = started.CurrentSection!.Questions[0];

            Assert.Equal(sessionId, duplicateStart.Id);
            await service.SaveResponseAsync(sessionId, firstQuestion.Id, firstQuestion.Options[0].Id);
            DiagnosticSessionView duplicateResponse = await service.SaveResponseAsync(
                sessionId,
                firstQuestion.Id,
                firstQuestion.Options[0].Id);
            Assert.Equal(1, duplicateResponse.AnsweredCount);
            DiagnosticSessionView completed = await service.CompleteSectionAsync(sessionId, 0);
            DiagnosticSessionView duplicateCompletion = await service.CompleteSectionAsync(sessionId, 0);
            Assert.Equal(completed.CurrentSectionIndex, duplicateCompletion.CurrentSectionIndex);
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(
            dataDirectory,
            timeProvider: clock);
        using var secondSource = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
        using var secondCoordinator = new DiagnosticSessionCoordinator();
        DiagnosticSessionService secondService = CreateService(secondRun, secondSource, secondCoordinator, clock);

        DiagnosticSessionView resumed = await secondService.GetAsync(sessionId);

        Assert.Equal(1, resumed.AnsweredCount);
        Assert.Equal(DiagnosticSectionStatus.Completed, resumed.Sections[0].Status);
        Assert.Equal(DiagnosticSectionStatus.Pending, resumed.Sections[1].Status);
        Assert.Equal(1, resumed.CurrentSectionIndex);
        var resumedProfile = await secondRun.GetRequiredService<ILocalProfileRepository>().GetAsync();
        DiagnosticSessionData resumedData = await secondRun
            .GetRequiredService<IDiagnosticSessionRepository>()
            .GetAsync(resumedProfile.LocalId, sessionId)
            ?? throw new InvalidOperationException("Session reprise absente.");
        Assert.Equal(
            questionIds,
            resumedData.Plan.Sections.SelectMany(section => section.Questions).Select(question => question.Id));
        await using var connection = new SqliteConnection($"Data Source={secondRun.DatabasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM DiagnosticResponses WHERE SessionId = $sessionId;";
        command.Parameters.AddWithValue("$sessionId", sessionId);
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ServerClockExpiresSectionAndRejectsLateResponse()
    {
        var clock = new AdjustableTimeProvider(Start);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
        using var coordinator = new DiagnosticSessionCoordinator();
        DiagnosticSessionService service = CreateService(environment, source, coordinator, clock, reducedSeconds: 60);
        DiagnosticSessionView started = await service.StartAsync(DiagnosticMode.Reduced);
        DiagnosticQuestionView question = started.CurrentSection!.Questions[0];

        clock.Advance(TimeSpan.FromSeconds(60));
        DiagnosticSessionView expired = await service.GetAsync(started.Id);

        Assert.Equal(DiagnosticSectionStatus.Expired, expired.Sections[0].Status);
        Assert.Equal(DiagnosticSectionStatus.Pending, expired.CurrentSection!.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveResponseAsync(
            started.Id,
            question.Id,
            question.Options[0].Id).AsTask());
    }

    [Fact]
    public async Task ExplicitFinishKeepsIncompleteCollectionState()
    {
        var clock = new AdjustableTimeProvider(Start);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
        using var coordinator = new DiagnosticSessionCoordinator();
        DiagnosticSessionService service = CreateService(environment, source, coordinator, clock);
        DiagnosticSessionView session = await service.StartAsync(DiagnosticMode.Reduced);
        for (int sectionIndex = 0; sectionIndex < 3; sectionIndex++)
        {
            session = await service.CompleteSectionAsync(session.Id, sectionIndex);
            if (sectionIndex < 2)
            {
                session = await service.StartCurrentSectionAsync(session.Id);
            }
        }

        DiagnosticSessionView finished = await service.FinishAsync(session.Id);
        DiagnosticSessionView duplicateFinish = await service.FinishAsync(session.Id);

        Assert.Equal(DiagnosticSessionStatus.Completed, finished.Status);
        Assert.False(finished.IsComplete);
        Assert.Equal(finished.EndedAtUtc, duplicateFinish.EndedAtUtc);
        Assert.Equal(0, finished.AnsweredCount);
    }

    private static DiagnosticSessionService CreateService(
        PersistenceTestEnvironment environment,
        FileSystemDiagnosticBankSource source,
        DiagnosticSessionCoordinator coordinator,
        TimeProvider clock,
        int reducedSeconds = 120) => new(
            source,
            environment.GetRequiredService<IDiagnosticSessionRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            new DiagnosticSessionOptions
            {
                InitialSectionDuration = TimeSpan.FromMinutes(30),
                ReducedSectionDuration = TimeSpan.FromSeconds(reducedSeconds),
            },
            clock);

    private sealed class AdjustableTimeProvider(DateTimeOffset current) : TimeProvider
    {
        private DateTimeOffset _current = current;

        public override DateTimeOffset GetUtcNow() => _current;

        public void Advance(TimeSpan duration) => _current = _current.Add(duration);
    }
}
