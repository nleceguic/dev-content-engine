using DevContentEngine.Domain.Entities;
using DevContentEngine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence;

public class UnitOfWorkTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

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
    public async Task SaveChangesAsync_persists_everything_staged_on_the_shared_DbContext()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);

        context.GitHubRepositories.Add(new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow));

        var affectedRows = await unitOfWork.SaveChangesAsync();

        affectedRows.Should().Be(1);
    }
}
