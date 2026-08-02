using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

public interface ISqlLabGateway
{
    Task<SqlLabAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default);

    Task<SqlLabSessionDescriptor> CreateSessionAsync(CancellationToken cancellationToken = default);

    Task<SqlLabSessionDescriptor> ResetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<SqlLabExecutionResult> ExecuteAsync(
        Guid sessionId,
        string query,
        SqlLabExpectedResult? expectation,
        CancellationToken cancellationToken = default);
}
