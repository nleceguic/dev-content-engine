using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DevContentEngine.Application.EventHandlers;

public sealed class GenerationRunCompletedWithoutContentEventHandler
    : INotificationHandler<DomainEventNotification<GenerationRunCompletedWithoutContentEvent>>
{
    private const string DefaultReason = "No se especificó un motivo.";

    private readonly INotifier _notifier;
    private readonly ILogger<GenerationRunCompletedWithoutContentEventHandler> _logger;

    public GenerationRunCompletedWithoutContentEventHandler(INotifier notifier, ILogger<GenerationRunCompletedWithoutContentEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<GenerationRunCompletedWithoutContentEvent> notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notifier.NotifyNoContentApprovedAsync(notification.DomainEvent.Reason ?? DefaultReason, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send the no-content-generated notification for run {RunId}.",
                notification.DomainEvent.GenerationRunId);
        }
    }
}
