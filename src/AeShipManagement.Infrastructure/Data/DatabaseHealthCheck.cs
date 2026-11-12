using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AeShipManagement.Infrastructure.Data;

internal sealed class DatabaseHealthCheck(ISqlConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await connectionFactory.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection opened.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable.", ex);
        }
    }
}
