using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DevContentEngine.Application.EventHandlers;

public sealed class GenerationRunFailedEventHandler : INotificationHandler<DomainEventNotification<GenerationRunFailedEvent>>
{
    private readonly INotifier _notifier;
    private readonly ILogger<GenerationRunFailedEventHandler> _logger;

    public GenerationRunFailedEventHandler(INotifier notifier, ILogger<GenerationRunFailedEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<GenerationRunFailedEvent> notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notifier.NotifyPipelineFailedAsync(notification.DomainEvent.ErrorMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send the pipeline-failed notification for run {RunId}.",
                notification.DomainEvent.GenerationRunId);
        }
    }
}
