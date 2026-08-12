using DevContentEngine.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevContentEngine.Infrastructure.HealthChecks;

public sealed class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly DevContentEngineDbContext _dbContext;

    public PostgreSqlHealthCheck(DevContentEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Connected to PostgreSQL.")
                : HealthCheckResult.Unhealthy("Could not connect to PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Could not connect to PostgreSQL.", ex);
        }
    }
}
