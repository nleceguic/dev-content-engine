using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence.Repositories;

public class PromptVersionRepositoryTests : IAsyncLifetime
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
    public async Task GetActiveAsync_returns_the_active_prompt_for_the_given_role_only()
    {
        var now = DateTime.UtcNow;

        var activeGenerator = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, "Active generator prompt", now);
        var inactiveGenerator = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, "Old generator prompt", now);
        inactiveGenerator.Deactivate();
        var activeReviewer = new PromptVersion(Guid.NewGuid(), PromptRole.Reviewer, "Active reviewer prompt", now);

        await using (var context = CreateContext())
        {
            var repository = new PromptVersionRepository(context);
            await repository.AddAsync(activeGenerator);
            await repository.AddAsync(inactiveGenerator);
            await repository.AddAsync(activeReviewer);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var readRepo = new PromptVersionRepository(readContext);

        var result = await readRepo.GetActiveAsync(PromptRole.Generator);

        result.Should().NotBeNull();
        result!.Id.Should().Be(activeGenerator.Id);
    }

    [Fact]
    public async Task GetActiveAsync_returns_null_when_no_active_prompt_exists_for_the_role()
    {
        var inactiveGenerator = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, "Old generator prompt", DateTime.UtcNow);
        inactiveGenerator.Deactivate();

        await using (var context = CreateContext())
        {
            await new PromptVersionRepository(context).AddAsync(inactiveGenerator);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var result = await new PromptVersionRepository(readContext).GetActiveAsync(PromptRole.Generator);

        result.Should().BeNull();
    }
}
