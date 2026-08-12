using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Projects;

namespace ForgeDotNet.Application.Projects;

public sealed record SubmitProjectCommand(
    Guid RequestId,
    string ProjectId,
    IReadOnlyList<CodeRunSourceFile> SourceFiles);

/// <summary>Résultat d'une suite d'acceptation, rattaché à son jalon.</summary>
public sealed record ProjectSuiteOutcome(string MilestoneId, string Title, CodeRunResult Result);

public sealed record ProjectSubmissionResult(
    ProjectSubmission Submission,
    IReadOnlyList<ProjectSuiteOutcome> Suites);

/// <summary>
/// Exécute toutes les suites d'acceptation d'un projet sur une même soumission, puis persiste une
/// observation unique.
/// </summary>
/// <remarks>
/// Ce cas d'usage n'emprunte délibérément pas <c>RunExercise</c> : celui-ci écrit dans les
/// observations de pratique C#, et un projet y gonflerait un score qu'il n'a pas produit. Les deux
/// flux d'observation restent séparés.
///
/// La preuve automatique n'est retenue que si chaque suite a réellement rapporté des tests. Le mode
/// manuel n'en rapporte aucun : sa soumission est enregistrée comme déclarée, jamais comme réussie.
/// </remarks>
public sealed class SubmitProject(
    IProjectSource projects,
    ICodeRunner codeRunner,
    TimeProvider timeProvider,
    IProjectSubmissionRepository? submissions = null,
    ILocalProfileRepository? profileRepository = null)
{
    public async ValueTask<ProjectSubmissionResult> ExecuteAsync(
        SubmitProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        Project project = await projects.GetAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Projet introuvable.");
        if (!project.IsVerifiable)
        {
            throw new InvalidOperationException(
                "Ce projet est guidé sans être vérifiable : aucune suite d'acceptation ne lui est attachée.");
        }

        if (command.SourceFiles.Count > project.MaximumSourceFiles)
        {
            throw new InvalidOperationException(
                $"Ce projet accepte au plus {project.MaximumSourceFiles} fichiers.");
        }

        var outcomes = new List<ProjectSuiteOutcome>(project.AcceptanceSuites.Count);
        foreach (ProjectAcceptanceSuite suite in project.AcceptanceSuites)
        {
            var request = new CodeRunRequest(
                Guid.NewGuid(),
                ProjectAcceptanceSuite.RunIdentifier(project.Id, suite.MilestoneId),
                project.Version,
                project.Revision,
                command.SourceFiles);
            CodeRunContract.ValidateRequest(request);
            CodeRunResult result = CodeRunContract.NormalizeResult(
                request,
                await codeRunner.RunAsync(request, cancellationToken));
            outcomes.Add(new ProjectSuiteOutcome(suite.MilestoneId, TitleOf(project, suite), result));
        }

        Guid profileId = profileRepository is null
            ? Guid.Empty
            : (await profileRepository.GetAsync(cancellationToken)).LocalId;
        ProjectSubmission submission = Aggregate(command, project, outcomes, profileId);
        if (profileId != Guid.Empty)
        {
            submission.Validate();
            await PersistAsync(submission, cancellationToken);
        }

        return new ProjectSubmissionResult(submission, Array.AsReadOnly(outcomes.ToArray()));
    }

    private ProjectSubmission Aggregate(
        SubmitProjectCommand command,
        Project project,
        IReadOnlyList<ProjectSuiteOutcome> outcomes,
        Guid profileId)
    {
        int passedSuites = outcomes.Count(item => item.Result.Status == CodeRunStatus.Succeeded);
        int totalTests = outcomes.Sum(item => item.Result.Tests.TotalCount);
        int passedTests = outcomes.Sum(item => item.Result.Tests.PassedCount);

        // Une exécution réelle rapporte des tests. Le mode manuel n'en rapporte aucun : c'est le
        // même signal que celui retenu pour les observations de pratique.
        bool verified = outcomes.Count > 0
            && outcomes.All(item =>
                item.Result.Status != CodeRunStatus.Unavailable && item.Result.Tests.TotalCount > 0);

        return new ProjectSubmission(
            command.RequestId,
            profileId,
            project.Id,
            project.Version,
            project.Revision,
            Fingerprint(command.SourceFiles),
            Status(outcomes, passedSuites, verified),
            outcomes.Count,
            passedSuites,
            totalTests,
            passedTests,
            verified,
            timeProvider.GetUtcNow());
    }

    private static ProjectSubmissionStatus Status(
        IReadOnlyList<ProjectSuiteOutcome> outcomes,
        int passedSuites,
        bool verified)
    {
        if (!verified)
        {
            return ProjectSubmissionStatus.Declared;
        }

        if (passedSuites == outcomes.Count)
        {
            return ProjectSubmissionStatus.Succeeded;
        }

        // Le statut retenu est le plus grave rencontré : un projet dont une suite n'a pas compilé
        // n'est pas dans le même état qu'un projet dont une suite a simplement échoué.
        if (outcomes.Any(item => item.Result.Status == CodeRunStatus.TimedOut))
        {
            return ProjectSubmissionStatus.TimedOut;
        }

        if (outcomes.Any(item => item.Result.Status == CodeRunStatus.Cancelled))
        {
            return ProjectSubmissionStatus.Cancelled;
        }

        return outcomes.Any(item => item.Result.Status == CodeRunStatus.CompilationFailed)
            ? ProjectSubmissionStatus.CompilationFailed
            : ProjectSubmissionStatus.TestsFailed;
    }

    private async ValueTask PersistAsync(ProjectSubmission submission, CancellationToken cancellationToken)
    {
        if (submissions is null)
        {
            return;
        }

        await submissions.AppendAsync(
            submission,
            cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken);
    }

    private static string TitleOf(Project project, ProjectAcceptanceSuite suite) =>
        project.Milestones
            .FirstOrDefault(item => string.Equals(item.Id, suite.MilestoneId, StringComparison.Ordinal))
            ?.Title
        ?? suite.MilestoneId;

    private static string Fingerprint(IReadOnlyList<CodeRunSourceFile> sourceFiles)
    {
        string canonical = JsonSerializer.Serialize(sourceFiles
            .OrderBy(item => item.FileName, StringComparer.Ordinal)
            .Select(item => new { item.FileName, item.Content }));
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";
    }
}
