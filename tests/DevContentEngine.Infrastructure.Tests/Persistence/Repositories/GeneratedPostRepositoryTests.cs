using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using DevContentEngine.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.Persistence.Repositories;

public class GeneratedPostRepositoryTests : IAsyncLifetime
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

    private static GeneratedPost Post(DateTime createdAt, ContentIdea contentIdea, PromptVersion prompt) =>
        new(
            Guid.NewGuid(),
            contentIdea.Id,
            "Hook",
            "Body",
            "Conclusion",
            "Cta",
            ["#dotnet"],
            ["https://github.com/owner/repo/commit/abc"],
            ContentOrigin.GitHub,
            prompt.Id,
            createdAt);

    private static (ContentIdea Idea, PromptVersion Prompt) SupportingEntities(DateTime now)
    {
        var idea = new ContentIdea(Guid.NewGuid(), ContentOrigin.GitHub, 10m, [Guid.NewGuid()], null, now, ContentPath.GitHubPath);
        var prompt = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, "Generate a post", now);

        return (idea, prompt);
    }

    [Fact]
    public async Task GetRecentPostsAsync_returns_only_posts_created_within_the_last_N_days()
    {
        var (idea, prompt) = SupportingEntities(Now);

        var withinWindow = Post(Now.AddDays(-5), idea, prompt);
        var onTheBoundary = Post(Now.AddDays(-14), idea, prompt);
        var outsideWindow = Post(Now.AddDays(-20), idea, prompt);

        await using (var context = CreateContext())
        {
            context.ContentIdeas.Add(idea);
            context.PromptVersions.Add(prompt);
            context.GeneratedPosts.AddRange(withinWindow, onTheBoundary, outsideWindow);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new GeneratedPostRepository(readContext, new FixedDateTimeProvider(Now));

        var result = await repository.GetRecentPostsAsync(14);

        result.Select(p => p.Id).Should().BeEquivalentTo([withinWindow.Id, onTheBoundary.Id]);
        result.Select(p => p.Id).Should().NotContain(outsideWindow.Id);
    }

    [Fact]
    public async Task GetRecentPostsAsync_used_by_the_topic_repetition_detector_returns_hashtags_for_overlap_comparison()
    {
        var (idea, prompt) = SupportingEntities(Now);
        var recentPost = Post(Now.AddDays(-2), idea, prompt);

        await using (var context = CreateContext())
        {
            context.ContentIdeas.Add(idea);
            context.PromptVersions.Add(prompt);
            context.GeneratedPosts.Add(recentPost);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new GeneratedPostRepository(readContext, new FixedDateTimeProvider(Now));

        var result = await repository.GetRecentPostsAsync(14);

        result.Should().ContainSingle();
        result.Single().Hashtags.Should().BeEquivalentTo(["#dotnet"]);
    }

    [Fact]
    public async Task GetRecentPostsAsync_throws_for_a_negative_day_count()
    {
        await using var context = CreateContext();
        var repository = new GeneratedPostRepository(context, new FixedDateTimeProvider(Now));

        var act = async () => await repository.GetRecentPostsAsync(-1);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetPendingReviewAsync_returns_only_Draft_and_Edited_posts()
    {
        var (idea, prompt) = SupportingEntities(Now);

        var draft = Post(Now, idea, prompt);
        var edited = Post(Now, idea, prompt);
        edited.Edit("Edited hook", "Edited body", "Edited conclusion", null, ["#kafka"], Now);
        var used = Post(Now, idea, prompt);
        used.MarkAsUsed(Now);
        var discarded = Post(Now, idea, prompt);
        discarded.Discard(Now);

        await using (var context = CreateContext())
        {
            context.ContentIdeas.Add(idea);
            context.PromptVersions.Add(prompt);
            context.GeneratedPosts.AddRange(draft, edited, used, discarded);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new GeneratedPostRepository(readContext, new FixedDateTimeProvider(Now));

        var result = await repository.GetPendingReviewAsync();

        result.Select(p => p.Id).Should().BeEquivalentTo([draft.Id, edited.Id]);
    }

    [Fact]
    public async Task GetLatestAsync_returns_the_most_recent_posts_newest_first_up_to_count()
    {
        var (idea, prompt) = SupportingEntities(Now);

        var oldest = Post(Now.AddDays(-3), idea, prompt);
        var middle = Post(Now.AddDays(-2), idea, prompt);
        var newest = Post(Now.AddDays(-1), idea, prompt);

        await using (var context = CreateContext())
        {
            context.ContentIdeas.Add(idea);
            context.PromptVersions.Add(prompt);
            context.GeneratedPosts.AddRange(oldest, middle, newest);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new GeneratedPostRepository(readContext, new FixedDateTimeProvider(Now));

        var result = await repository.GetLatestAsync(2);

        result.Select(p => p.Id).Should().ContainInOrder(newest.Id, middle.Id);
        result.Should().NotContain(p => p.Id == oldest.Id);
    }
}
