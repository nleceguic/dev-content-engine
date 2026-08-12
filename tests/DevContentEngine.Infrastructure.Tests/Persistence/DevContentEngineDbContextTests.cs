using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence;

public class DevContentEngineDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private DevContentEngineDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DevContentEngineDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new DevContentEngineDbContext(options);
    }

    [Fact]
    public async Task GitHubActivity_round_trips_its_detected_technologies_array()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", DateTime.UtcNow);
        var activity = new GitHubActivity(
            Guid.NewGuid(),
            repository.Id,
            GitHubActivityType.Commit,
            ["C#", "Docker"],
            "Implement pipeline",
            "sha-1",
            DateTime.UtcNow);
        activity.MarkAnalyzed(isNoise: false);

        await using (var writeContext = CreateContext())
        {
            writeContext.GitHubRepositories.Add(repository);
            writeContext.GitHubActivities.Add(activity);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await readContext.GitHubActivities.SingleAsync(a => a.Id == activity.Id);

        reloaded.DetectedTechnologies.Should().BeEquivalentTo(["C#", "Docker"]);
        reloaded.IsNoise.Should().BeFalse();
        reloaded.RepositoryId.Should().Be(repository.Id);
        reloaded.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GeneratedPost_round_trips_status_transitions_hashtags_and_sources()
    {
        var contentIdea = new ContentIdea(
            Guid.NewGuid(),
            ContentOrigin.GitHub,
            10m,
            [Guid.NewGuid()],
            relatedTrendId: null,
            DateTime.UtcNow,
            ContentPath.GitHubPath);

        var promptVersion = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, "Generate a post", DateTime.UtcNow);

        var post = new GeneratedPost(
            Guid.NewGuid(),
            contentIdea.Id,
            "Hook",
            "Body",
            "Conclusion",
            "Cta",
            ["#dotnet", "#postgres"],
            ["https://github.com/owner/repo/commit/abc"],
            "Dark navy background with glowing architecture nodes.",
            ContentOrigin.GitHub,
            promptVersion.Id,
            DateTime.UtcNow);

        post.Edit("New hook", "New body", "New conclusion", null, ["#kafka"], DateTime.UtcNow);

        await using (var writeContext = CreateContext())
        {
            writeContext.ContentIdeas.Add(contentIdea);
            writeContext.PromptVersions.Add(promptVersion);
            writeContext.GeneratedPosts.Add(post);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await readContext.GeneratedPosts.SingleAsync(p => p.Id == post.Id);

        reloaded.Status.Should().Be(GeneratedPostStatus.Edited);
        reloaded.Hook.Should().Be("New hook");
        reloaded.Hashtags.Should().BeEquivalentTo(["#kafka"]);
        reloaded.Sources.Should().BeEquivalentTo(["https://github.com/owner/repo/commit/abc"]);
        reloaded.Cta.Should().BeNull();
        reloaded.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerationRun_round_trips_a_failed_run()
    {
        var run = new GenerationRun(Guid.NewGuid(), DateTime.UtcNow);
        run.Fail("GitHub API unavailable", DateTime.UtcNow);

        await using (var writeContext = CreateContext())
        {
            writeContext.GenerationRuns.Add(run);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await readContext.GenerationRuns.SingleAsync(r => r.Id == run.Id);

        reloaded.Status.Should().Be(GenerationRunStatus.Failed);
        reloaded.ErrorMessage.Should().Be("GitHub API unavailable");
        reloaded.ResultingPostId.Should().BeNull();
    }

    [Fact]
    public async Task ContentIdea_round_trips_related_activity_ids_array()
    {
        var activityId1 = Guid.NewGuid();
        var activityId2 = Guid.NewGuid();

        var contentIdea = new ContentIdea(
            Guid.NewGuid(),
            ContentOrigin.GitHub,
            8m,
            [activityId1, activityId2],
            relatedTrendId: null,
            DateTime.UtcNow,
            ContentPath.GitHubPath);

        await using (var writeContext = CreateContext())
        {
            writeContext.ContentIdeas.Add(contentIdea);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await readContext.ContentIdeas.SingleAsync(i => i.Id == contentIdea.Id);

        reloaded.RelatedActivityIds.Should().BeEquivalentTo([activityId1, activityId2]);
    }
}
