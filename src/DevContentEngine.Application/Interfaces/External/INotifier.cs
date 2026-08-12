using DevContentEngine.Application.Interfaces.External.Models;

namespace DevContentEngine.Application.Interfaces.External;

public interface INotifier
{
    Task NotifyDraftReadyAsync(DraftReadyNotification notification, CancellationToken cancellationToken = default);

    Task NotifyPipelineFailedAsync(string reason, CancellationToken cancellationToken = default);

    Task NotifyNoContentApprovedAsync(string reason, CancellationToken cancellationToken = default);
}
