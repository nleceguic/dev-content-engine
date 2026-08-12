using DevContentEngine.Domain.Common;

namespace DevContentEngine.Application.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default);
}
