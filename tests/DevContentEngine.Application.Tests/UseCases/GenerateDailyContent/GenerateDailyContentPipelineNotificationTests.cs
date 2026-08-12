using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.UseCases.GenerateDailyContent;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DevContentEngine.Application.Tests.UseCases.GenerateDailyContent;

public class GenerateDailyContentPipelineNotificationTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

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
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<INotifier> _notifier = new();

    private ISender BuildSender()
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

        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var services = new ServiceCollection();

        services.AddApplication();
        services.AddLogging();

        services.AddSingleton(_gitHubClient.Object);
        services.AddSingleton(_trendSource.Object);
        services.AddSingleton(_contentGenerationService.Object);
        services.AddSingleton(_repositoryRepository.Object);
        services.AddSingleton(_activityRepository.Object);
        services.AddSingleton(_contentIdeaRepository.Object);
        services.AddSingleton(_generatedPostRepository.Object);
        services.AddSingleton(_generationRunRepository.Object);
        services.AddSingleton(_trendRepository.Object);
        services.AddSingleton(_unitOfWork.Object);
        services.AddSingleton(_dateTimeProvider.Object);
        services.AddSingleton(_notifier.Object);

        services.AddSingleton<ActivityNoiseFilter>();
        services.AddSingleton<TechnologyExtractor>();
        services.AddSingleton<ActivityScorer>();
        services.AddSingleton<TopicRepetitionDetector>();

        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    [Fact]
    public async Task Handle_dispatches_a_pipeline_failed_notification_end_to_end_when_the_LLM_provider_fails()
    {
        const string owner = "nleceguic";
        const string llmErrorMessage = "LLM API returned 503 (ServiceUnavailable): Overloaded";

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
            .ThrowsAsync(new LlmGenerationException(llmErrorMessage));

        var sender = BuildSender();

        var result = await sender.Send(new GenerateDailyContentCommand(owner));

        result.Status.Should().Be(GenerationRunStatus.Failed);
        result.ErrorMessage.Should().Be(llmErrorMessage);

        _notifier.Verify(
            notifier => notifier.NotifyPipelineFailedAsync(llmErrorMessage, It.IsAny<CancellationToken>()),
            Times.Once);
        _notifier.Verify(
            notifier => notifier.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifier.Verify(
            notifier => notifier.NotifyNoContentApprovedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
