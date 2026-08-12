using ForgeDotNet.Domain.Projects;

namespace ForgeDotNet.Application.Projects;

public interface IProjectSource
{
    ValueTask<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<Project?> GetAsync(string projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrouve le projet et le jalon désignés par un identifiant d'exécution de suite, de la forme
    /// <c>&lt;projet&gt;.&lt;jalon&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Le découpage se fait sur le dernier point : un identifiant de projet en contient lui-même,
    /// et c'est le suffixe qui désigne le jalon.
    /// </remarks>
    ValueTask<(Project Project, ProjectAcceptanceSuite Suite)?> FindSuiteAsync(
        string runIdentifier,
        CancellationToken cancellationToken = default);
}

public interface IProjectSubmissionRepository
{
    ValueTask AppendAsync(ProjectSubmission submission, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ProjectSubmission>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
