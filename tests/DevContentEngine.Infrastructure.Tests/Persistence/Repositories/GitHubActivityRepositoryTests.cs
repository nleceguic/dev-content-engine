using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence.Repositories;

public class GitHubActivityRepositoryTests : IAsyncLifetime
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

    private static GitHubActivity Commit(Guid repositoryId, string sha, DateTime timestamp) =>
        new(Guid.NewGuid(), repositoryId, GitHubActivityType.Commit, ["C#"], $"Commit {sha}", sha, timestamp);

    [Fact]
    public async Task AddAsync_and_GetByIdAsync_round_trip_a_single_activity()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow);
        var activity = Commit(repository.Id, "sha-1", DateTime.UtcNow);

        await using (var context = CreateContext())
        {
            var repositoryRepo = new GitHubRepositoryRepository(context);
            var activityRepo = new GitHubActivityRepository(context);

            await repositoryRepo.AddAsync(repository);
            await activityRepo.AddAsync(activity);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new GitHubActivityRepository(readContext);

        var found = await readRepo.GetByIdAsync(activity.Id);

        found.Should().NotBeNull();
        found!.ExternalId.Should().Be("sha-1");
        found.DetectedTechnologies.Should().BeEquivalentTo(["C#"]);
    }

    [Fact]
    public async Task AddRangeAsync_inserts_several_activities_in_one_call()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow);
        var activities = new[]
        {
            Commit(repository.Id, "sha-a", DateTime.UtcNow),
            Commit(repository.Id, "sha-b", DateTime.UtcNow),
            Commit(repository.Id, "sha-c", DateTime.UtcNow)
        };

        await using (var context = CreateContext())
        {
            await new GitHubRepositoryRepository(context).AddAsync(repository);
            await new GitHubActivityRepository(context).AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var count = await readContext.GitHubActivities.CountAsync(a => a.RepositoryId == repository.Id);

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetByExternalIdAsync_finds_an_activity_and_returns_null_when_missing()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow);
        var activity = Commit(repository.Id, "sha-unique-123", DateTime.UtcNow);

        await using (var context = CreateContext())
        {
            await new GitHubRepositoryRepository(context).AddAsync(repository);
            await new GitHubActivityRepository(context).AddAsync(activity);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new GitHubActivityRepository(readContext);

        var found = await readRepo.GetByExternalIdAsync("sha-unique-123");
        var missing = await readRepo.GetByExternalIdAsync("does-not-exist");

        found.Should().NotBeNull();
        found!.Id.Should().Be(activity.Id);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentByRepositoryAsync_returns_only_activities_within_the_date_window_for_that_repository()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow);
        var otherRepository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "other-repo", DateTime.UtcNow);

        var referenceDate = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);
        var sinceUtc = referenceDate.AddDays(-7);

        var withinWindow = Commit(repository.Id, "sha-within", referenceDate.AddDays(-3));
        var onTheBoundary = Commit(repository.Id, "sha-boundary", sinceUtc);
        var beforeWindow = Commit(repository.Id, "sha-before", referenceDate.AddDays(-10));
        var wrongRepository = Commit(otherRepository.Id, "sha-other-repo", referenceDate.AddDays(-1));

        await using (var context = CreateContext())
        {
            var repositoryRepo = new GitHubRepositoryRepository(context);
            await repositoryRepo.AddAsync(repository);
            await repositoryRepo.AddAsync(otherRepository);

            var activityRepo = new GitHubActivityRepository(context);
            await activityRepo.AddRangeAsync([withinWindow, onTheBoundary, beforeWindow, wrongRepository]);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new GitHubActivityRepository(readContext);

        var result = await readRepo.GetRecentByRepositoryAsync(repository.Id, sinceUtc);

        result.Select(a => a.ExternalId).Should().BeEquivalentTo(["sha-within", "sha-boundary"]);
    }
}
