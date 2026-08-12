using DevContentEngine.Domain.Entities;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence.Repositories;

public class GitHubRepositoryRepositoryTests : IAsyncLifetime
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
    public async Task GetByOwnerAndNameAsync_finds_a_repository_and_returns_null_when_missing()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow);

        await using (var context = CreateContext())
        {
            await new GitHubRepositoryRepository(context).AddAsync(repository);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new GitHubRepositoryRepository(readContext);

        var found = await readRepo.GetByOwnerAndNameAsync("nleceguic", "dev-content-engine");
        var missing = await readRepo.GetByOwnerAndNameAsync("nleceguic", "does-not-exist");

        found.Should().NotBeNull();
        found!.Id.Should().Be(repository.Id);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveRepositoriesAsync_excludes_deactivated_repositories()
    {
        var active = new GitHubRepository(Guid.NewGuid(), "nleceguic", "active-repo", DateTime.UtcNow);
        var inactive = new GitHubRepository(Guid.NewGuid(), "nleceguic", "inactive-repo", DateTime.UtcNow);
        inactive.Deactivate();

        await using (var context = CreateContext())
        {
            var repository = new GitHubRepositoryRepository(context);
            await repository.AddAsync(active);
            await repository.AddAsync(inactive);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new GitHubRepositoryRepository(readContext);

        var result = await readRepo.GetActiveRepositoriesAsync();

        result.Select(r => r.Id).Should().Contain(active.Id);
        result.Select(r => r.Id).Should().NotContain(inactive.Id);
    }
}
