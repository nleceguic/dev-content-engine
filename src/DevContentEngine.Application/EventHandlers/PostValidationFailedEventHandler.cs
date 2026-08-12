using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DevContentEngine.Application.EventHandlers;

public sealed class PostValidationFailedEventHandler : INotificationHandler<DomainEventNotification<PostValidationFailedEvent>>
{
    private readonly INotifier _notifier;
    private readonly ILogger<PostValidationFailedEventHandler> _logger;

    public PostValidationFailedEventHandler(INotifier notifier, ILogger<PostValidationFailedEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PostValidationFailedEvent> notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notifier.NotifyNoContentApprovedAsync(notification.DomainEvent.Reason, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send the no-content-approved notification for post {PostId}.",
                notification.DomainEvent.GeneratedPostId);
        }
    }
}
