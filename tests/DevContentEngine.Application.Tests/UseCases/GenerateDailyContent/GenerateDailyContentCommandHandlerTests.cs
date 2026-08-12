using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.UseCases.GenerateDailyContent;
using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.GenerateDailyContent;

public class GenerateDailyContentCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid GeneratorPromptVersionId = Guid.NewGuid();

    private readonly Mock<IGitHubClient> _gitHubClient = new();
    private readonly Mock<ITrendSource> _trendSource = new();
    private readonly Mock<IContentGenerationService> _contentGenerationService = new();
    private readonly Mock<IGitHubRepositoryRepository> _repositoryRepository = new();
    private readonly Mock<IGitHubActivityRepository> _activityRepository = new();
    private readonly Mock<IContentIdeaRepository> _contentIdeaRepository = new();
    private readonly Mock<IGeneratedPostRepository> _generatedPostRepository = new();
    private readonly Mock<IGenerationRunRepository> _generationRunRepository = new();
    private readonly Mock<ITrendRepository> _trendRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcher = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public GenerateDailyContentCommandHandlerTests()
    {
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);

        _generationRunRepository
            .Setup(repository => repository.GetLastRunTimestampAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        _repositoryRepository
            .Setup(repository => repository.GetByOwnerAndNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitHubRepository?)null);

        _generatedPostRepository
            .Setup(repository => repository.GetRecentPostsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GeneratedPost>());

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _contentGenerationService
            .Setup(service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedContentResult("Hook", "Body", "Conclusion", "Cta", ["#dotnet"], ["source"], GeneratorPromptVersionId));
    }

    private GenerateDailyContentCommandHandler CreateHandler()
    {
        return new GenerateDailyContentCommandHandler(
            _gitHubClient.Object,
            _trendSource.Object,
            _contentGenerationService.Object,
            _repositoryRepository.Object,
            _activityRepository.Object,
            _contentIdeaRepository.Object,
            _generatedPostRepository.Object,
            _generationRunRepository.Object,
            _trendRepository.Object,
            _unitOfWork.Object,
            _domainEventDispatcher.Object,
            _dateTimeProvider.Object,
            new ActivityNoiseFilter(),
            new TechnologyExtractor(),
            new ActivityScorer(),
            new TopicRepetitionDetector());
    }

    private GenerationRunCapture CaptureGenerationRun()
    {
        var capture = new GenerationRunCapture();

        _generationRunRepository
            .Setup(repository => repository.AddAsync(It.IsAny<GenerationRun>(), It.IsAny<CancellationToken>()))
            .Callback<GenerationRun, CancellationToken>((run, _) => capture.Value = run)
            .Returns(Task.CompletedTask);

        return capture;
    }

    private sealed class GenerationRunCapture
    {
        public GenerationRun? Value { get; set; }
    }

    private static GitHubUserActivity EmptyActivity() => new([], [], [], []);

    [Fact]
    public async Task Handle_chooses_GitHubPath_and_persists_a_successful_run_when_activity_is_strong()
    {
        const string owner = "nleceguic";
        const string repositoryName = "dev-content-engine";

        var commits = Enumerable.Range(0, 4)
            .Select(day => new GitHubCommitSnapshot(
                owner,
                repositoryName,
                Sha: $"sha-{day}",
                Message: $"Implement pipeline step {day}",
                CommittedAt: Now.AddDays(-day),
                FilesChanged: 3,
                LinesChanged: 30,
                ChangedFilePaths: ["src/Worker/Program.cs"]))
            .ToList();

        _gitHubClient
            .Setup(client => client.GetUserActivityAsync(owner, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubUserActivity([], commits, [], []));

        var capturedRun = CaptureGenerationRun();
        ContentGenerationRequest? capturedRequest = null;

        _contentGenerationService
            .Setup(service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ContentGenerationRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new GeneratedContentResult("Hook", "Body", "Conclusion", "Cta", ["#dotnet"], ["source"], GeneratorPromptVersionId));

        var handler = CreateHandler();

        var result = await handler.Handle(new GenerateDailyContentCommand(owner), CancellationToken.None);

        result.Status.Should().Be(GenerationRunStatus.Success);
        result.GeneratedPostId.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();

        capturedRun.Value.Should().NotBeNull();
        capturedRun.Value!.Id.Should().Be(result.GenerationRunId);
        capturedRun.Value.Status.Should().Be(GenerationRunStatus.Success);
        capturedRun.Value.ChosenPath.Should().Be(ContentPath.GitHubPath);
        capturedRun.Value.ResultingPostId.Should().Be(result.GeneratedPostId);
        capturedRun.Value.FinishedAt.Should().NotBeNull();

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Activities.Should().HaveCount(4);
        capturedRequest.Repositories.Should().ContainSingle(r => r.Owner == owner && r.Name == repositoryName);
        capturedRequest.Trend.Should().BeNull();

        _trendSource.Verify(
            source => source.GetTrendsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _contentIdeaRepository.Verify(repository => repository.AddAsync(It.IsAny<ContentIdea>(), It.IsAny<CancellationToken>()), Times.Once);
        _generatedPostRepository.Verify(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            dispatcher => dispatcher.DispatchAndClearEventsAsync(It.IsAny<IEnumerable<Entity>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_persists_the_GeneratedPost_with_the_PromptVersionId_returned_by_the_content_generation_service()
    {
        const string owner = "nleceguic";

        var commits = Enumerable.Range(0, 4)
            .Select(day => new GitHubCommitSnapshot(
                owner,
                "dev-content-engine",
                Sha: $"sha-{day}",
                Message: $"Implement pipeline step {day}",
                CommittedAt: Now.AddDays(-day),
                FilesChanged: 3,
                LinesChanged: 30,
                ChangedFilePaths: ["src/Worker/Program.cs"]))
            .ToList();

        _gitHubClient
            .Setup(client => client.GetUserActivityAsync(owner, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubUserActivity([], commits, [], []));

        GeneratedPost? capturedPost = null;

        _generatedPostRepository
            .Setup(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()))
            .Callback<GeneratedPost, CancellationToken>((post, _) => capturedPost = post)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        await handler.Handle(new GenerateDailyContentCommand(owner), CancellationToken.None);

        capturedPost.Should().NotBeNull();
        capturedPost!.PromptVersionId.Should().Be(GeneratorPromptVersionId);
    }

    [Fact]
    public async Task Handle_chooses_TrendPath_and_persists_a_successful_run_when_GitHub_activity_is_weak()
    {
        var rawActivity = new GitHubUserActivity(
            [],
            [new GitHubCommitSnapshot("owner", "repo", "sha-1", "wip", Now, 1, 5, [])],
            [],
            []);

        _gitHubClient
            .Setup(client => client.GetUserActivityAsync("nleceguic", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawActivity);

        _trendSource
            .Setup(source => source.GetTrendsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TrendCandidate("New C# feature", "dev.to", "https://dev.to/x", Now.AddDays(-1))]);

        var capturedRun = CaptureGenerationRun();
        ContentGenerationRequest? capturedRequest = null;

        _contentGenerationService
            .Setup(service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ContentGenerationRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new GeneratedContentResult("Hook", "Body", "Conclusion", "Cta", ["#dotnet"], ["source"], GeneratorPromptVersionId));

        var handler = CreateHandler();

        var result = await handler.Handle(new GenerateDailyContentCommand("nleceguic"), CancellationToken.None);

        result.Status.Should().Be(GenerationRunStatus.Success);
        result.GeneratedPostId.Should().NotBeNull();

        capturedRun.Value.Should().NotBeNull();
        capturedRun.Value!.ChosenPath.Should().Be(ContentPath.TrendPath);
        capturedRun.Value.ResultingPostId.Should().Be(result.GeneratedPostId);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Trend.Should().NotBeNull();
        capturedRequest.Trend!.Title.Should().Be("New C# feature");

        _trendRepository.Verify(repository => repository.AddAsync(It.IsAny<Trend>(), It.IsAny<CancellationToken>()), Times.Once);
        _generatedPostRepository.Verify(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_NoContentGenerated_with_a_clear_reason_and_persists_the_run_when_TrendPath_has_no_trends()
    {
        _gitHubClient
            .Setup(client => client.GetUserActivityAsync("nleceguic", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyActivity());

        _trendSource
            .Setup(source => source.GetTrendsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TrendCandidate>());

        var capturedRun = CaptureGenerationRun();

        var handler = CreateHandler();

        var act = async () => await handler.Handle(new GenerateDailyContentCommand("nleceguic"), CancellationToken.None);

        var result = await act.Should().NotThrowAsync();

        result.Subject.Status.Should().Be(GenerationRunStatus.NoContentGenerated);
        result.Subject.GeneratedPostId.Should().BeNull();
        result.Subject.ErrorMessage.Should().Be("No trend candidates were found for the configured keywords.");

        capturedRun.Value.Should().NotBeNull();
        capturedRun.Value!.Status.Should().Be(GenerationRunStatus.NoContentGenerated);
        capturedRun.Value.ErrorMessage.Should().Be("No trend candidates were found for the configured keywords.");
        capturedRun.Value.ResultingPostId.Should().BeNull();

        _contentGenerationService.Verify(
            service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _generatedPostRepository.Verify(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_NoContentGenerated_when_the_content_generation_service_returns_null()
    {
        const string owner = "nleceguic";

        var commits = Enumerable.Range(0, 4)
            .Select(day => new GitHubCommitSnapshot(
                owner,
                "dev-content-engine",
                Sha: $"sha-{day}",
                Message: $"Implement pipeline step {day}",
                CommittedAt: Now.AddDays(-day),
                FilesChanged: 3,
                LinesChanged: 30,
                ChangedFilePaths: ["src/Worker/Program.cs"]))
            .ToList();

        _gitHubClient
            .Setup(client => client.GetUserActivityAsync(owner, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubUserActivity([], commits, [], []));

        _contentGenerationService
            .Setup(service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeneratedContentResult?)null);

        var capturedRun = CaptureGenerationRun();

        var handler = CreateHandler();

        var result = await handler.Handle(new GenerateDailyContentCommand(owner), CancellationToken.None);

        result.Status.Should().Be(GenerationRunStatus.NoContentGenerated);
        result.GeneratedPostId.Should().BeNull();
        result.ErrorMessage.Should().Be("Content generation did not produce a post that passed validation.");

        capturedRun.Value.Should().NotBeNull();
        capturedRun.Value!.Status.Should().Be(GenerationRunStatus.NoContentGenerated);
        capturedRun.Value.ErrorMessage.Should().Be("Content generation did not produce a post that passed validation.");

        _generatedPostRepository.Verify(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_records_a_failed_run_without_an_unhandled_exception_when_the_GitHub_API_fails()
    {
        _gitHubClient
            .Setup(client => client.GetUserActivityAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("GitHub GraphQL API is unavailable"));

        var capturedRun = CaptureGenerationRun();

        var handler = CreateHandler();

        var result = await handler.Handle(new GenerateDailyContentCommand("nleceguic"), CancellationToken.None);

        result.Status.Should().Be(GenerationRunStatus.Failed);
        result.ErrorMessage.Should().Contain("GitHub GraphQL API is unavailable");
        result.GeneratedPostId.Should().BeNull();

        capturedRun.Value.Should().NotBeNull();
        capturedRun.Value!.Status.Should().Be(GenerationRunStatus.Failed);
        capturedRun.Value.ErrorMessage.Should().Contain("GitHub GraphQL API is unavailable");
        capturedRun.Value.ResultingPostId.Should().BeNull();
        capturedRun.Value.FinishedAt.Should().NotBeNull();

        _trendSource.Verify(
            source => source.GetTrendsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _contentGenerationService.Verify(
            service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _generatedPostRepository.Verify(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_records_a_failed_run_with_the_LLM_providers_message_when_content_generation_throws()
    {
        const string owner = "nleceguic";

        var commits = Enumerable.Range(0, 4)
            .Select(day => new GitHubCommitSnapshot(
                owner,
                "dev-content-engine",
                Sha: $"sha-{day}",
                Message: $"Implement pipeline step {day}",
                CommittedAt: Now.AddDays(-day),
                FilesChanged: 3,
                LinesChanged: 30,
                ChangedFilePaths: ["src/Worker/Program.cs"]))
            .ToList();

        _gitHubClient
            .Setup(client => client.GetUserActivityAsync(owner, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubUserActivity([], commits, [], []));

        _contentGenerationService
            .Setup(service => service.GenerateAsync(It.IsAny<ContentGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmGenerationException("LLM API returned 503 (ServiceUnavailable): Overloaded"));

        var capturedRun = CaptureGenerationRun();

        var handler = CreateHandler();

        var act = async () => await handler.Handle(new GenerateDailyContentCommand(owner), CancellationToken.None);

        var result = await act.Should().NotThrowAsync();

        result.Subject.Status.Should().Be(GenerationRunStatus.Failed);
        result.Subject.ErrorMessage.Should().Be("LLM API returned 503 (ServiceUnavailable): Overloaded");
        result.Subject.GeneratedPostId.Should().BeNull();

        capturedRun.Value.Should().NotBeNull();
        capturedRun.Value!.Status.Should().Be(GenerationRunStatus.Failed);
        capturedRun.Value.ErrorMessage.Should().Be("LLM API returned 503 (ServiceUnavailable): Overloaded");
        capturedRun.Value.ResultingPostId.Should().BeNull();

        _generatedPostRepository.Verify(repository => repository.AddAsync(It.IsAny<GeneratedPost>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            dispatcher => dispatcher.DispatchAndClearEventsAsync(
                It.Is<IEnumerable<Entity>>(entities => entities.Contains(capturedRun.Value)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
