using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Infrastructure.SqlLab;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ForgeDotNet.Web.Health;

public sealed class SqlLabHealthCheck(ISqlLabGateway gateway, SqlLabOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        SqlLabAvailability availability = await gateway.GetAvailabilityAsync(cancellationToken);
        return availability.Available || !options.Enabled
            ? HealthCheckResult.Healthy(availability.Message, new Dictionary<string, object>
            {
                ["available"] = availability.Available,
            })
            : HealthCheckResult.Unhealthy(availability.Message);
    }
}
