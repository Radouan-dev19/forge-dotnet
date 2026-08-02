using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ForgeDotNet.Web.Health;

public sealed class LocalProfileHealthCheck(LocalDatabaseHealthProbe databaseHealthProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await databaseHealthProbe.CheckAsync(cancellationToken);
            return health.IsHealthy
                ? HealthCheckResult.Healthy(health.Description)
                : HealthCheckResult.Unhealthy(health.Description, health.Exception);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
    }
}
