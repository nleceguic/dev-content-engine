using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DevContentEngine.Application.EventHandlers;

public sealed class DraftReadyEventHandler : INotificationHandler<DomainEventNotification<DraftReadyEvent>>
{
    private const int TopicMaxLength = 60;

    private readonly IGeneratedPostRepository _generatedPostRepository;
    private readonly IContentIdeaRepository _contentIdeaRepository;
    private readonly ITrendRepository _trendRepository;
    private readonly IGitHubRepositoryRepository _repositoryRepository;
    private readonly INotifier _notifier;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DraftReadyEventHandler> _logger;

    public DraftReadyEventHandler(
        IGeneratedPostRepository generatedPostRepository,
        IContentIdeaRepository contentIdeaRepository,
        ITrendRepository trendRepository,
        IGitHubRepositoryRepository repositoryRepository,
        INotifier notifier,
        IDateTimeProvider dateTimeProvider,
        ILogger<DraftReadyEventHandler> logger)
    {
        _generatedPostRepository = generatedPostRepository;
        _contentIdeaRepository = contentIdeaRepository;
        _trendRepository = trendRepository;
        _repositoryRepository = repositoryRepository;
        _notifier = notifier;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<DraftReadyEvent> notification, CancellationToken cancellationToken)
    {
        var postId = notification.DomainEvent.GeneratedPostId;
        var post = await _generatedPostRepository.GetByIdAsync(postId, cancellationToken);

        if (post is null)
        {
            _logger.LogWarning("DraftReadyEvent fired for GeneratedPost {PostId}, but it could not be found.", postId);
            return;
        }

        var contentIdea = await _contentIdeaRepository.GetByIdAsync(post.ContentIdeaId, cancellationToken);
        var (origin, reason) = await BuildOriginAndReasonAsync(contentIdea, cancellationToken);

        var draftNotification = new DraftReadyNotification(
            post.Id,
            BuildTopic(post.Hook),
            origin,
            reason,
            post.Hook,
            post.Body,
            post.Conclusion,
            post.Cta,
            post.ImagePrompt,
            _dateTimeProvider.UtcNow);

        try
        {
            await _notifier.NotifyDraftReadyAsync(draftNotification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the draft-ready notification for post {PostId}.", post.Id);
        }
    }

    private async Task<(string Origin, string Reason)> BuildOriginAndReasonAsync(ContentIdea? contentIdea, CancellationToken cancellationToken)
    {
        if (contentIdea is null)
        {
            return ("Desconocido", "No se encontró la idea de contenido asociada a este borrador.");
        }

        if (contentIdea.Origin == ContentOrigin.Trend && contentIdea.RelatedTrendId is { } trendId)
        {
            var trend = await _trendRepository.GetByIdAsync(trendId, cancellationToken);

            return trend is null
                ? ("Trend", $"Tendencia relevante (score de actividad {contentIdea.ActivityScore:F1}).")
                : ($"Trend/{trend.Source}", $"Tendencia relevante: {trend.Title}.");
        }

        if (contentIdea.Origin == ContentOrigin.RepoHighlight && contentIdea.RelatedRepositoryId is { } repositoryId)
        {
            var repository = await _repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
            var repositoryName = repository?.FullName ?? "repositorio desconocido";

            return (
                $"Repo Highlight / {repositoryName}",
                "Sin actividad reciente relevante — destacando una característica existente de este proyecto.");
        }

        return ("GitHub", $"Actividad de GitHub con score {contentIdea.ActivityScore:F1}, por encima del umbral configurado.");
    }

    private static string BuildTopic(string hook)
    {
        if (hook.Length <= TopicMaxLength)
        {
            return hook;
        }

        var truncated = hook[..TopicMaxLength];
        var lastSpace = truncated.LastIndexOf(' ');

        return (lastSpace > 0 ? truncated[..lastSpace] : truncated) + "…";
    }
}
