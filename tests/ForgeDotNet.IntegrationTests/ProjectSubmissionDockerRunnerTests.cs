using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Projects;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.Projects;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Practice;
using ForgeDotNet.Infrastructure.Projects;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Parcourt le trajet réel d'une soumission de projet : sources rendues, conteneur isolé, suites
/// d'acceptation exécutées, statut agrégé.
/// </summary>
/// <remarks>
/// <para>
/// Cette classe comble un angle mort relevé lors du rejeu de l'audit du 17 août 2026. Deux garanties
/// existaient de part et d'autre du trajet sans que rien ne les relie :
/// <c>ProjectCorrectnessTests</c> prouve hors Docker que chaque suite est franchissable, et
/// <c>DockerCodeRunnerSecurityTests</c> prouve que le bac à sable tient sa politique. Aucune ne
/// prouvait qu'une soumission de projet traverse effectivement le bac à sable et en ressort avec le
/// bon verdict.
/// </para>
/// <para>
/// L'écart n'était pas théorique : le rejeu a montré que le runner ne pouvait créer aucun conteneur
/// sur un moteur Docker à jour, l'option de montage <c>bind-nonrecursive</c> ayant été supprimée. La
/// vérification hors ligne restait verte pendant que le produit était inutilisable. C'est exactement
/// le genre de défaut qu'une preuve de bout en bout attrape et qu'une preuve par morceaux laisse
/// passer.
/// </para>
/// <para>
/// Ce qui reste hors de portée d'un test : l'éditeur du navigateur, et l'affichage de
/// l'accomplissement dans le tableau de progression. Le trajet est donc vérifié de la soumission au
/// verdict, pas de la frappe au pixel — et le rapport d'audit le dit plutôt que de laisser croire au
/// contraire.
/// </para>
/// </remarks>
[Collection(EfDockerCodeRunnerTestGroup.CollectionName)]
[Trait("Category", "ProjectSubmissionRunner")]
public sealed class ProjectSubmissionDockerRunnerTests(DockerSecurityFixture dockerFixture)
{
    private const string ProjectId = "project-orders-database-001";

    [Fact]
    public async Task TheReferenceSolutionPassesEverySuiteInsideTheIsolatedRunner()
    {
        using var context = await ProjectRunContext.CreateAsync(dockerFixture);
        Project project = await context.Projects.GetAsync(ProjectId)
            ?? throw new InvalidDataException($"{ProjectId} absent du catalogue.");

        Assert.True(project.IsVerifiable);
        Assert.Equal(3, project.AcceptanceSuites.Count);

        ProjectSubmissionResult result = await context.Submit.ExecuteAsync(new SubmitProjectCommand(
            Guid.NewGuid(),
            ProjectId,
            [context.ReadReferenceSolution()]));

        // Le détail par suite avant l'agrégat : un échec doit nommer le jalon fautif, sinon la
        // reprise repart d'un « ça ne passe pas » sans point d'entrée.
        foreach (ProjectSuiteOutcome outcome in result.Suites)
        {
            Assert.True(
                outcome.Result.Status == CodeRunStatus.Succeeded,
                $"{outcome.MilestoneId} : statut={outcome.Result.Status}, "
                + $"compilation={outcome.Result.Compilation.Output.Text}, "
                + $"tests={outcome.Result.Tests.Output.Text}");
            Assert.True(outcome.Result.Tests.TotalCount > 0);
            Assert.Equal(outcome.Result.Tests.TotalCount, outcome.Result.Tests.PassedCount);
        }

        Assert.Equal(ProjectSubmissionStatus.Succeeded, result.Submission.Status);
        Assert.True(result.Submission.AutomaticallyVerified);
        Assert.Equal(result.Submission.TotalSuites, result.Submission.PassedSuites);

        // Le corrigé de ce projet exécute EF Core contre SQLite dans le conteneur. Le prouver ici
        // vaut mieux que de le déduire du manifeste : c'est la seule preuve que les assemblies
        // approuvées suffisent réellement à l'exécution, et non seulement au chargement.
        Assert.Equal(MasteryPolicyKey, project.AchievementKey);
    }

    [Fact]
    public async Task TheStarterFailsAndTheSubmissionIsNotVerifiedAsSucceeded()
    {
        using var context = await ProjectRunContext.CreateAsync(dockerFixture);

        ProjectSubmissionResult result = await context.Submit.ExecuteAsync(new SubmitProjectCommand(
            Guid.NewGuid(),
            ProjectId,
            [context.ReadStarter()]));

        Assert.NotEqual(ProjectSubmissionStatus.Succeeded, result.Submission.Status);
        Assert.True(result.Submission.PassedSuites < result.Submission.TotalSuites);

        // Le squelette compile — il doit échouer sur ses cas, pas sur sa syntaxe : un projet dont le
        // squelette ne compile pas apprend à réparer une erreur de frappe, pas à écrire des requêtes.
        Assert.All(result.Suites, outcome => Assert.True(
            outcome.Result.Status == CodeRunStatus.TestsFailed,
            $"{outcome.MilestoneId} : statut={outcome.Result.Status}, "
            + $"compilation={outcome.Result.Compilation.Output.Text}"));

        // Aucun secret de correction ne remonte : les cas cachés restent nommés sans être détaillés.
        Assert.All(result.Suites, outcome => Assert.DoesNotContain(
            "/workspace",
            outcome.Result.Tests.Output.Text,
            StringComparison.OrdinalIgnoreCase));
    }

    private const string MasteryPolicyKey = "ef-core";

    private sealed class ProjectRunContext : IDisposable
    {
        private ProjectRunContext(
            ContentCatalogProvider provider,
            FileSystemProjectSource projects,
            DockerCodeRunner runner,
            SubmitProject submit,
            string catalogRoot,
            string workspace)
        {
            _provider = provider;
            _runner = runner;
            _workspace = workspace;
            Projects = projects;
            Submit = submit;
            CatalogRoot = catalogRoot;
        }

        private readonly ContentCatalogProvider _provider;
        private readonly DockerCodeRunner _runner;
        private readonly string _workspace;

        public FileSystemProjectSource Projects { get; }

        public SubmitProject Submit { get; }

        public string CatalogRoot { get; }

        public static async Task<ProjectRunContext> CreateAsync(DockerSecurityFixture fixture)
        {
            string contentRoot = FindContentRoot();
            string catalogRoot = Path.Combine(contentRoot, "reference");
            var validationOptions = new ContentValidationOptions { ContentRootPath = contentRoot };
            var provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
                new FileSystemContentValidationService(validationOptions),
                validationOptions));
            ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogRoot);
            Assert.True(
                reload.Succeeded,
                string.Join(Environment.NewLine, reload.Issues.Select(item => item.Message)));

            var contentOptions = new PracticeContentOptions
            {
                ContentRootPath = contentRoot,
                CatalogDirectoryPath = catalogRoot,
            };
            var practiceSource = new FileSystemPracticeExerciseSource(provider, contentOptions);
            var projects = new FileSystemProjectSource(provider, contentOptions);
            var specificationSource = new FileSystemDockerRunSpecificationSource(
                practiceSource,
                new DockerRunContentOptions
                {
                    ContentRootPath = contentRoot,
                    CatalogDirectoryPath = catalogRoot,
                },
                debugScenarioSource: null,
                projectSource: projects);
            string workspace = Path.Combine(
                Path.GetTempPath(), "ForgeDotNet.ProjectRunner", Guid.NewGuid().ToString("N"));
            var runner = new DockerCodeRunner(
                new DockerCodeRunnerOptions
                {
                    DockerContext = fixture.DockerContext,
                    ImageReference = fixture.ImageReference,
                    WorkspaceRootPath = workspace,
                    MaximumConcurrency = 1,
                    // Trente secondes est le plafond de la plage de sécurité, pas un réglage de
                    // confort : le produit ne permet pas d'allonger un délai pour faire passer un
                    // projet, et ce test s'exécute donc sous la contrainte réelle.
                    TestTimeout = TimeSpan.FromSeconds(30),
                },
                specificationSource,
                TimeProvider.System);

            return new ProjectRunContext(
                provider,
                projects,
                runner,
                new SubmitProject(projects, runner, TimeProvider.System),
                catalogRoot,
                workspace);
        }

        public CodeRunSourceFile ReadReferenceSolution() => Read("solution");

        public CodeRunSourceFile ReadStarter() => Read("starter");

        private CodeRunSourceFile Read(string directory) => new(
            "Submission.cs",
            File.ReadAllText(Path.Combine(CatalogRoot, "projects", ProjectId, directory, "Submission.cs")));

        public void Dispose()
        {
            _runner.Dispose();
            Projects.Dispose();
            _provider.Dispose();
            if (Directory.Exists(_workspace))
            {
                Directory.Delete(_workspace, recursive: true);
            }
        }

        private static string FindContentRoot()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
                directory is not null;
                directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "content");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Racine de contenu introuvable.");
        }
    }
}
