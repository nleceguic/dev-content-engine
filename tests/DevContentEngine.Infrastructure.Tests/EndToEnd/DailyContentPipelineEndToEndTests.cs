using DevContentEngine.Application;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.UseCases.GenerateDailyContent;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Infrastructure.Llm.Prompts;
using DevContentEngine.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;

namespace DevContentEngine.Infrastructure.Tests.EndToEnd;

public class DailyContentPipelineEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<DevContentEngineDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var context = new DevContentEngineDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Handle_persists_a_valid_Draft_GeneratedPost_for_a_realistic_GitHub_activity()
    {
        const string username = "nleceguic";
        var now = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

        var commits = Enumerable.Range(0, 5)
            .Select(day => new GitHubCommitSnapshot(
                username,
                "dev-content-engine",
                Sha: $"sha-{day}",
                Message: $"Implement pipeline stage {day}",
                CommittedAt: now.AddDays(-day),
                FilesChanged: 4,
                LinesChanged: 80,
                ChangedFilePaths: ["src/Worker/Program.cs", "src/Infrastructure/GitHub/GitHubClient.cs"]))
            .ToList();

        var mergedPullRequest = new GitHubPullRequestSnapshot(
            username,
            "dev-content-engine",
            ExternalId: "12",
            Title: "Add PostgreSQL persistence",
            MergedAt: now.AddDays(-1),
            ChangedFilePaths: ["src/Infrastructure/Persistence/DevContentEngineDbContext.cs"]);

        var rawActivity = new GitHubUserActivity([], commits, [mergedPullRequest], []);

        var gitHubClient = new Mock<IGitHubClient>();
        gitHubClient
            .Setup(client => client.GetUserActivityAsync(username, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawActivity);

        var trendSource = new Mock<ITrendSource>();
        trendSource
            .Setup(source => source.GetTrendsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TrendCandidate>());

        const string hook = "Terminé de conectar el pipeline diario con GitHub y Postgres.";
        const string conclusion = "Fue un buen ejercicio de diseño en capas y trazabilidad.";
        const string cta = "¿Qué opinas del enfoque?";
        var body = new string('a', Math.Max(1, 1000 - hook.Length - conclusion.Length - cta.Length));
        var validDraft = new GeneratorResponsePayload(hook, body, conclusion, cta, ["#dotnet", "#postgres"], ["sha-0"]);

        var llmProvider = new Mock<ILlmProvider>();
        llmProvider
            .Setup(provider => provider.GenerateStructuredAsync<GeneratorResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validDraft);

        var notifier = new Mock<INotifier>();

        var connectionString = _postgres.GetConnectionString();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["GitHub:Token"] = "test-token",
                ["Llm:ApiKey"] = "test-key",
                ["Telegram:BotToken"] = "test-bot-token",
                ["Telegram:ChatId"] = "12345"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddApplication();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        services.RemoveAll<IGitHubClient>();
        services.AddSingleton(gitHubClient.Object);
        services.RemoveAll<ITrendSource>();
        services.AddSingleton(trendSource.Object);
        services.RemoveAll<ILlmProvider>();
        services.AddSingleton(llmProvider.Object);
        services.RemoveAll<INotifier>();
        services.AddSingleton(notifier.Object);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GenerateDailyContentCommand(username));

        result.ErrorMessage.Should().BeNull();
        result.Status.Should().Be(GenerationRunStatus.Success);
        result.GeneratedPostId.Should().NotBeNull();

        await using var readContext = new DevContentEngineDbContext(new DbContextOptionsBuilder<DevContentEngineDbContext>()
            .UseNpgsql(connectionString)
            .Options);

        var persistedPost = await readContext.GeneratedPosts.SingleAsync(post => post.Id == result.GeneratedPostId);

        persistedPost.Status.Should().Be(GeneratedPostStatus.Draft);
        persistedPost.Hook.Should().Be(hook);
        persistedPost.Body.Should().Be(body);
        persistedPost.Hashtags.Should().BeEquivalentTo(["#dotnet", "#postgres"]);
        persistedPost.Sources.Should().NotBeEmpty();
        persistedPost.Origin.Should().Be(ContentOrigin.GitHub);

        var persistedRun = await readContext.GenerationRuns.SingleAsync(run => run.Id == result.GenerationRunId);
        persistedRun.Status.Should().Be(GenerationRunStatus.Success);
        persistedRun.ChosenPath.Should().Be(ContentPath.GitHubPath);
        persistedRun.ResultingPostId.Should().Be(result.GeneratedPostId);

        notifier.Verify(
            n => n.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
