using DevContentEngine.Domain.Entities;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using DevContentEngine.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence.Repositories;

public class TrendRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private DevContentEngineDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DevContentEngineDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new DevContentEngineDbContext(options);
    }

    [Fact]
    public async Task GetRecentAsync_returns_only_trends_published_within_the_last_N_days()
    {
        var recent = new Trend(Guid.NewGuid(), "Recent trend", "dev.to", "https://dev.to/recent", Now.AddDays(-2), 1.0m);
        var old = new Trend(Guid.NewGuid(), "Old trend", "dev.to", "https://dev.to/old", Now.AddDays(-10), 1.0m);

        await using (var context = CreateContext())
        {
            var repository = new TrendRepository(context, new FixedDateTimeProvider(Now));
            await repository.AddAsync(recent);
            await repository.AddAsync(old);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new TrendRepository(readContext, new FixedDateTimeProvider(Now));

        var result = await readRepo.GetRecentAsync(7);

        result.Select(t => t.Id).Should().Contain(recent.Id);
        result.Select(t => t.Id).Should().NotContain(old.Id);
    }
}
