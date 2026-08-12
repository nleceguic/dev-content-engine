using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Application.Tests.TestHelpers;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DevContentEngine.Application.Tests.EventHandlers;

public class NotificationHandlersIntegrationTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<INotifier> _notifier = new();
    private readonly Mock<IGeneratedPostRepository> _generatedPostRepository = new();
    private readonly Mock<IContentIdeaRepository> _contentIdeaRepository = new();
    private readonly Mock<ITrendRepository> _trendRepository = new();
    private readonly Mock<IGitHubRepositoryRepository> _repositoryRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private IPublisher BuildPublisher()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddLogging();

        services.AddSingleton(_notifier.Object);
        services.AddSingleton(_generatedPostRepository.Object);
        services.AddSingleton(_contentIdeaRepository.Object);
        services.AddSingleton(_trendRepository.Object);
        services.AddSingleton(_repositoryRepository.Object);
        services.AddSingleton(_dateTimeProvider.Object);

        return services.BuildServiceProvider().GetRequiredService<IPublisher>();
    }

    [Fact]
    public async Task Publishing_a_DraftReadyEvent_triggers_NotifyDraftReadyAsync_exactly_once()
    {
        var post = GeneratedPostTestFactory.Create();
        var contentIdea = new ContentIdea(
            post.ContentIdeaId,
            ContentOrigin.GitHub,
            activityScore: 7.5m,
            relatedActivityIds: [Guid.NewGuid()],
            relatedTrendId: null,
            Now,
            ContentPath.GitHubPath);

        _generatedPostRepository.Setup(repository => repository.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        _contentIdeaRepository.Setup(repository => repository.GetByIdAsync(post.ContentIdeaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentIdea);
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);

        var publisher = BuildPublisher();

        await publisher.Publish(new DomainEventNotification<DraftReadyEvent>(new DraftReadyEvent(post.Id)));

        _notifier.Verify(
            notifier => notifier.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _notifier.Verify(
            notifier => notifier.NotifyPipelineFailedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifier.Verify(
            notifier => notifier.NotifyNoContentApprovedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Publishing_a_DraftReadyEvent_for_a_RepoHighlight_post_reports_the_repo_highlight_origin_and_reason()
    {
        var post = GeneratedPostTestFactory.Create();
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "rush-order", Now.AddDays(-90));
        var contentIdea = new ContentIdea(
            post.ContentIdeaId,
            ContentOrigin.RepoHighlight,
            activityScore: 0m,
            relatedActivityIds: [],
            relatedTrendId: null,
            Now,
            ContentPath.RepoHighlightPath,
            relatedRepositoryId: repository.Id);

        _generatedPostRepository.Setup(repository => repository.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        _contentIdeaRepository.Setup(repository => repository.GetByIdAsync(post.ContentIdeaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentIdea);
        _repositoryRepository.Setup(repo => repo.GetByIdAsync(repository.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);

        var publisher = BuildPublisher();

        DraftReadyNotification? capturedNotification = null;
        _notifier
            .Setup(notifier => notifier.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()))
            .Callback<DraftReadyNotification, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        await publisher.Publish(new DomainEventNotification<DraftReadyEvent>(new DraftReadyEvent(post.Id)));

        capturedNotification.Should().NotBeNull();
        capturedNotification!.Origin.Should().Be("Repo Highlight / nleceguic/rush-order");
        capturedNotification.Reason.Should().Be(
            "Sin actividad reciente relevante — destacando una característica existente de este proyecto.");
    }

    [Fact]
    public async Task Publishing_a_PostValidationFailedEvent_triggers_NotifyNoContentApprovedAsync_exactly_once()
    {
        var publisher = BuildPublisher();

        await publisher.Publish(new DomainEventNotification<PostValidationFailedEvent>(
            new PostValidationFailedEvent(Guid.NewGuid(), "Missing a verifiable source.")));

        _notifier.Verify(
            notifier => notifier.NotifyNoContentApprovedAsync("Missing a verifiable source.", It.IsAny<CancellationToken>()),
            Times.Once);
        _notifier.Verify(
            notifier => notifier.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifier.Verify(
            notifier => notifier.NotifyPipelineFailedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Publishing_a_GenerationRunCompletedWithoutContentEvent_triggers_NotifyNoContentApprovedAsync_exactly_once()
    {
        var publisher = BuildPublisher();

        await publisher.Publish(new DomainEventNotification<GenerationRunCompletedWithoutContentEvent>(
            new GenerationRunCompletedWithoutContentEvent(Guid.NewGuid(), "No trend candidates were found.")));

        _notifier.Verify(
            notifier => notifier.NotifyNoContentApprovedAsync("No trend candidates were found.", It.IsAny<CancellationToken>()),
            Times.Once);
        _notifier.Verify(
            notifier => notifier.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifier.Verify(
            notifier => notifier.NotifyPipelineFailedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Publishing_a_GenerationRunFailedEvent_triggers_NotifyPipelineFailedAsync_exactly_once()
    {
        var publisher = BuildPublisher();

        await publisher.Publish(new DomainEventNotification<GenerationRunFailedEvent>(
            new GenerationRunFailedEvent(Guid.NewGuid(), "GitHub API is unavailable.")));

        _notifier.Verify(
            notifier => notifier.NotifyPipelineFailedAsync("GitHub API is unavailable.", It.IsAny<CancellationToken>()),
            Times.Once);
        _notifier.Verify(
            notifier => notifier.NotifyDraftReadyAsync(It.IsAny<DraftReadyNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifier.Verify(
            notifier => notifier.NotifyNoContentApprovedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
