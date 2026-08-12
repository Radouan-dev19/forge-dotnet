using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

public interface ISqlLabGateway
{
    Task<SqlLabAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée une session jetable. Sans provisionnement, la base de démonstration du laboratoire est
    /// utilisée ; avec un provisionnement, c'est le jeu de données du scénario demandé.
    /// </summary>
    Task<SqlLabSessionDescriptor> CreateSessionAsync(
        SqlLabProvisioning? provisioning = null,
        CancellationToken cancellationToken = default);

    Task<SqlLabSessionDescriptor> ResetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<SqlLabExecutionResult> ExecuteAsync(
        Guid sessionId,
        string query,
        SqlLabExpectedResult? expectation,
        CancellationToken cancellationToken = default);
}
