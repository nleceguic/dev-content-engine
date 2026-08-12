using DevContentEngine.Infrastructure.HealthChecks;
using DevContentEngine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.HealthChecks;

public class PostgreSqlHealthCheckTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private DevContentEngineDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DevContentEngineDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DevContentEngineDbContext(options);
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Healthy_when_the_database_is_reachable()
    {
        await using var context = CreateContext(_postgres.GetConnectionString());
        var healthCheck = new PostgreSqlHealthCheck(context);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Unhealthy_when_the_database_is_unreachable()
    {
        const string unreachableConnectionString =
            "Host=127.0.0.1;Port=1;Database=postgres;Username=postgres;Password=postgres;Timeout=2";

        await using var context = CreateContext(unreachableConnectionString);
        var healthCheck = new PostgreSqlHealthCheck(context);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
