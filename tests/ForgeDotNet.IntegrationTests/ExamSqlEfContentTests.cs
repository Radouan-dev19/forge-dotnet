using System.Text.Json;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Exams;
using ForgeDotNet.Infrastructure.Practice;
using ForgeDotNet.Infrastructure.SqlLab;
using Microsoft.Extensions.Logging.Abstractions;

namespace ForgeDotNet.IntegrationTests;

[Collection("SqlEfContentSerial")]
[Trait("Category", "ContentS1S10")]
public sealed class ExamSqlEfContentTests
{
    [Fact]
    public async Task ExamFourContainsRealSqlAndEfCandidatesWithoutPrivateAnswers()
    {
        using ExamContentEnvironment environment = await ExamContentEnvironment.CreateAsync();
        ExamBlueprint exam = await environment.Bank.GetAsync("sql-ef-core-v1")
            ?? throw new InvalidDataException("Examen 4 absent.");

        Assert.Equal(8, exam.DrawCount);
        Assert.Equal(8, exam.Candidates.Count);
        Assert.Equal(6, exam.Candidates.Count(item => item.SubmissionKind == ExamSubmissionKind.Sql));
        Assert.Equal(2, exam.Candidates.Count(item => item.SubmissionKind == ExamSubmissionKind.CSharp));
        Assert.All(exam.Candidates, item => Assert.Equal(ForgeDotNet.Domain.Mastery.MasteryDomain.Sql, item.Domain));

        string publicProjection = JsonSerializer.Serialize(exam);
        foreach (ExamCandidate sqlCandidate in exam.Candidates.Where(item => item.SubmissionKind == ExamSubmissionKind.Sql))
        {
            SqlExamItemDefinition definition = await ((IExamSqlItemSource)environment.Bank).GetAsync(
                sqlCandidate.ItemId,
                sqlCandidate.ItemVersion,
                sqlCandidate.ContentRevision) ?? throw new InvalidDataException("Définition SQL privée absente.");
            Assert.NotEmpty(definition.SolutionQuery);
            Assert.DoesNotContain(definition.SolutionQuery, publicProjection, StringComparison.Ordinal);
            Assert.NotEmpty(definition.ExpectedResult.Rows);
        }

        foreach (ExamCandidate efCandidate in exam.Candidates.Where(item => item.SubmissionKind == ExamSubmissionKind.CSharp))
        {
            string solutionPath = Path.Combine(
                environment.ContentRoot,
                "sql",
                efCandidate.ItemId,
                "exam",
                "solution",
                "Submission.cs");
            string solution = await File.ReadAllTextAsync(solutionPath);
            var request = new CodeRunRequest(
                Guid.NewGuid(),
                efCandidate.ItemId,
                efCandidate.ItemVersion,
                efCandidate.ContentRevision,
                [new CodeRunSourceFile("Submission.cs", solution)]);
            DockerRunSpecification? specification = await environment.RunnerSource.GetAsync(request);
            Assert.NotNull(specification);
            Assert.NotNull(specification.SuiteDefinition);
            Assert.DoesNotContain("solution", specification.SuiteDefinition, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SqlExamRunnerUsesPrivateExpectationAndAlwaysDestroysSession()
    {
        var definition = new SqlExamItemDefinition(
            "exam-sql-test-001",
            1,
            new string('A', 64),
            new SqlLabExpectedResult(
                ["Value"],
                [[new SqlLabCell("42")]],
                Ordered: true,
                NumericTolerance: 0m),
            "SELECT 42 AS Value;");
        var source = new StaticSqlItemSource(definition);
        var gateway = new RecordingSqlGateway(validationPassed: true);
        var runner = new SqlLabExamRunner(source, gateway);
        var item = new ExamItemSnapshot(
            1,
            definition.ItemId,
            definition.ItemVersion,
            definition.ContentRevision,
            ForgeDotNet.Domain.Mastery.MasteryDomain.Sql,
            "SQL",
            "Retournez 42.",
            [],
            "Submission.sql",
            "SELECT 0 AS Value;",
            ExamSubmissionKind.Sql);

        ExamRunResult result = await runner.RunAsync(item, definition.SolutionQuery);

        Assert.Equal(ExamSubmissionOutcome.Succeeded, result.Outcome);
        Assert.Equal(2, result.TotalTests);
        Assert.Equal(2, result.PassedTests);
        Assert.Equal(0, result.HiddenFailureCount);
        Assert.Equal(1, gateway.CreateCount);
        Assert.Equal(1, gateway.DestroyCount);
        Assert.Same(definition.ExpectedResult, gateway.ReceivedExpectation);
    }

    [Fact]
    [Trait("Category", "SqlLabExam")]
    public async Task EverySqlExamSolutionPassesAndStarterFailsOnDisposableSqlLab()
    {
        using ExamContentEnvironment environment = await ExamContentEnvironment.CreateAsync();
        string repositoryRoot = Directory.GetParent(environment.ContentRoot)!.FullName;
        string secretPath = Path.Combine(repositoryRoot, ".secrets", "sql-lab-sa-password.txt");
        var options = new SqlLabOptions
        {
            Enabled = true,
            Server = "127.0.0.1",
            Port = 14333,
            AdministratorPasswordFile = secretPath,
            QueryTimeoutSeconds = 3,
            MaximumRows = 100,
            MaximumResultBytes = 65_536,
            MaximumSessions = 2,
            MaximumConcurrency = 1,
        };
        await using var gateway = new SqlServerLabGateway(
            options,
            TimeProvider.System,
            NullLogger<SqlServerLabGateway>.Instance);
        SqlLabAvailability availability = await gateway.GetAvailabilityAsync();
        Assert.True(availability.Available, availability.Message);
        var runner = new SqlLabExamRunner(environment.Bank, gateway);
        ExamBlueprint exam = await environment.Bank.GetAsync("sql-ef-core-v1")
            ?? throw new InvalidDataException("Examen 4 absent.");
        ExamCandidate[] candidates = exam.Candidates
            .Where(item => item.SubmissionKind == ExamSubmissionKind.Sql)
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(6, candidates.Length);

        foreach ((ExamCandidate candidate, int index) in candidates.Select((item, index) => (item, index)))
        {
            SqlExamItemDefinition definition = await ((IExamSqlItemSource)environment.Bank).GetAsync(
                candidate.ItemId,
                candidate.ItemVersion,
                candidate.ContentRevision) ?? throw new InvalidDataException("Définition SQL privée absente.");
            var item = new ExamItemSnapshot(
                index + 1,
                candidate.ItemId,
                candidate.ItemVersion,
                candidate.ContentRevision,
                candidate.Domain,
                candidate.Title,
                candidate.Statement,
                candidate.Constraints,
                candidate.StarterFileName,
                candidate.StarterCode,
                candidate.SubmissionKind);

            ExamRunResult starter = await runner.RunAsync(item, candidate.StarterCode);
            Assert.Equal(ExamSubmissionOutcome.TestsFailed, starter.Outcome);
            Assert.True(starter.HiddenFailureCount > 0);

            ExamRunResult solution = await runner.RunAsync(item, definition.SolutionQuery);
            Assert.Equal(ExamSubmissionOutcome.Succeeded, solution.Outcome);
            Assert.Equal(solution.TotalTests, solution.PassedTests);
            Assert.Equal(0, solution.HiddenFailureCount);
        }
    }

    private sealed class StaticSqlItemSource(SqlExamItemDefinition definition) : IExamSqlItemSource
    {
        public ValueTask<SqlExamItemDefinition?> GetAsync(
            string itemId,
            int itemVersion,
            string contentRevision,
            CancellationToken cancellationToken = default) => ValueTask.FromResult<SqlExamItemDefinition?>(
                itemId == definition.ItemId
                && itemVersion == definition.ItemVersion
                && contentRevision == definition.ContentRevision
                    ? definition
                    : null);
    }

    private sealed class RecordingSqlGateway(bool validationPassed) : ISqlLabGateway
    {
        private readonly Guid _sessionId = Guid.NewGuid();

        public int CreateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public SqlLabExpectedResult? ReceivedExpectation { get; private set; }

        /// <summary>L'examen doit provisionner son propre jeu de données, jamais un scénario publié.</summary>
        public SqlLabProvisioning? ReceivedProvisioning { get; private set; }

        public Task<SqlLabAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqlLabAvailability(true, "Disponible"));

        public Task<SqlLabSessionDescriptor> CreateSessionAsync(
            SqlLabProvisioning? provisioning = null,
            CancellationToken cancellationToken = default)
        {
            CreateCount++;
            ReceivedProvisioning = provisioning;
            return Task.FromResult(new SqlLabSessionDescriptor(
                _sessionId,
                1,
                DateTimeOffset.UtcNow,
                "dbo.Orders",
                new SqlLabLimits(3, 100, 65_536, 16_384)));
        }

        public Task<SqlLabSessionDescriptor> ResetSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(_sessionId, sessionId);
            DestroyCount++;
            return Task.CompletedTask;
        }

        public Task<SqlLabExecutionResult> ExecuteAsync(
            Guid sessionId,
            string query,
            SqlLabExpectedResult? expectation,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(_sessionId, sessionId);
            Assert.DoesNotContain("DROP", query, StringComparison.OrdinalIgnoreCase);
            ReceivedExpectation = expectation;
            return Task.FromResult(new SqlLabExecutionResult(
                SqlLabExecutionStatus.Succeeded,
                new SqlLabResultSet([new SqlLabColumn("Value", "int", false)], [[new SqlLabCell("42")]]),
                [],
                new SqlLabValidationResult(validationPassed, validationPassed ? [] : ["Valeur incorrecte"]),
                "Résultat borné.",
                Guid.NewGuid(),
                TimeSpan.FromMilliseconds(5)));
        }
    }

    private sealed class ExamContentEnvironment : IDisposable
    {
        private readonly ContentCatalogProvider _provider;

        private ExamContentEnvironment(
            string contentRoot,
            ContentCatalogProvider provider,
            FileSystemExamBankSource bank,
            FileSystemDockerRunSpecificationSource runnerSource)
        {
            ContentRoot = contentRoot;
            _provider = provider;
            Bank = bank;
            RunnerSource = runnerSource;
        }

        public string ContentRoot { get; }

        public FileSystemExamBankSource Bank { get; }

        public FileSystemDockerRunSpecificationSource RunnerSource { get; }

        public static async Task<ExamContentEnvironment> CreateAsync()
        {
            string contentRoot = FindContentRoot();
            string catalogRoot = Path.Combine(contentRoot, "reference");
            var validationOptions = new ContentValidationOptions { ContentRootPath = contentRoot };
            var provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
                new FileSystemContentValidationService(validationOptions),
                validationOptions));
            ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogRoot);
            Assert.True(reload.Succeeded, string.Join(Environment.NewLine, reload.Issues.Select(item => item.Message)));
            var practiceSource = new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
            {
                ContentRootPath = contentRoot,
                CatalogDirectoryPath = catalogRoot,
            });
            var bank = new FileSystemExamBankSource(practiceSource, new ExamBankOptions
            {
                ContentRootPath = contentRoot,
                BankDirectoryPath = Path.Combine(contentRoot, "exams"),
            });
            var runnerSource = new FileSystemDockerRunSpecificationSource(
                practiceSource,
                new DockerRunContentOptions
                {
                    ContentRootPath = contentRoot,
                    CatalogDirectoryPath = catalogRoot,
                    SqlDirectoryPath = Path.Combine(contentRoot, "sql"),
                });
            return new ExamContentEnvironment(contentRoot, provider, bank, runnerSource);
        }

        public void Dispose() => _provider.Dispose();

        private static string FindContentRoot()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "content");
                if (File.Exists(Path.Combine(candidate, "schemas", "lesson.schema.json"))) return candidate;
            }

            throw new DirectoryNotFoundException("Racine content introuvable.");
        }
    }
}
