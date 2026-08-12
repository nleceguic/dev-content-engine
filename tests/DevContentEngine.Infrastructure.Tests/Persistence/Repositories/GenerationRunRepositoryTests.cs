using System.Reflection;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence.Repositories;

public class GenerationRunRepositoryTests : IAsyncLifetime
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

    [Theory]
    [InlineData(typeof(IGenerationRunRepository))]
    [InlineData(typeof(GenerationRunRepository))]
    public void GenerationRuns_is_append_only_no_Update_method_is_exposed(Type repositoryType)
    {
        var hasUpdateMethod = repositoryType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Concat(repositoryType.GetInterfaces().SelectMany(i => i.GetMethods()))
            .Any(method => method.Name.Contains("Update", StringComparison.OrdinalIgnoreCase));

        hasUpdateMethod.Should().BeFalse(because: "GenerationRuns must only ever be appended to, never updated in place");
    }

    [Fact]
    public async Task AddAsync_then_a_single_SaveChanges_persists_the_runs_final_state_as_one_row()
    {
        var run = new GenerationRun(Guid.NewGuid(), DateTime.UtcNow);
        run.Fail("GitHub API unavailable", DateTime.UtcNow);

        await using (var context = CreateContext())
        {
            var repository = new GenerationRunRepository(context);
            await repository.AddAsync(run);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var rowCount = await readContext.GenerationRuns.CountAsync(r => r.Id == run.Id);
        var reloaded = await readContext.GenerationRuns.SingleAsync(r => r.Id == run.Id);

        rowCount.Should().Be(1);
        reloaded.Status.Should().Be(GenerationRunStatus.Failed);
        reloaded.ErrorMessage.Should().Be("GitHub API unavailable");
    }

    [Fact]
    public async Task GetLastRunTimestampAsync_returns_null_when_no_runs_exist()
    {
        await using var context = CreateContext();
        var repository = new GenerationRunRepository(context);

        var result = await repository.GetLastRunTimestampAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLastRunTimestampAsync_returns_the_most_recent_StartedAt()
    {
        var oldest = new GenerationRun(Guid.NewGuid(), new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc));
        oldest.CompleteWithoutContent(oldest.StartedAt.AddMinutes(5));

        var mostRecent = new GenerationRun(Guid.NewGuid(), new DateTime(2026, 8, 9, 8, 0, 0, DateTimeKind.Utc));
        mostRecent.CompleteWithoutContent(mostRecent.StartedAt.AddMinutes(5));

        await using (var context = CreateContext())
        {
            var repository = new GenerationRunRepository(context);
            await repository.AddAsync(oldest);
            await repository.AddAsync(mostRecent);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepository = new GenerationRunRepository(readContext);

        var result = await readRepository.GetLastRunTimestampAsync();

        result.Should().Be(mostRecent.StartedAt);
    }

    [Fact]
    public async Task GetLatestAsync_returns_the_most_recently_started_runs_up_to_count_newest_first()
    {
        var oldest = new GenerationRun(Guid.NewGuid(), new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc));
        oldest.CompleteWithoutContent(oldest.StartedAt.AddMinutes(5));

        var middle = new GenerationRun(Guid.NewGuid(), new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc));
        middle.Fail("GitHub API unavailable", middle.StartedAt.AddMinutes(1));

        var mostRecent = new GenerationRun(Guid.NewGuid(), new DateTime(2026, 8, 9, 8, 0, 0, DateTimeKind.Utc));
        mostRecent.CompleteWithoutContent(mostRecent.StartedAt.AddMinutes(3));

        await using (var context = CreateContext())
        {
            var repository = new GenerationRunRepository(context);
            await repository.AddAsync(oldest);
            await repository.AddAsync(middle);
            await repository.AddAsync(mostRecent);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepository = new GenerationRunRepository(readContext);

        var result = await readRepository.GetLatestAsync(2);

        result.Should().HaveCount(2);
        result.Select(run => run.Id).Should().ContainInOrder(mostRecent.Id, middle.Id);
    }

    [Fact]
    public async Task GetLatestAsync_returns_an_empty_collection_when_no_runs_exist()
    {
        await using var context = CreateContext();
        var repository = new GenerationRunRepository(context);

        var result = await repository.GetLatestAsync(5);

        result.Should().BeEmpty();
    }
}
