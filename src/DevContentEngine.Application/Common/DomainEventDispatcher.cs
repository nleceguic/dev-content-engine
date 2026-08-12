using DevContentEngine.Domain.Common;
using MediatR;

namespace DevContentEngine.Application.Common;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        var domainEvents = entityList.SelectMany(entity => entity.DomainEvents).ToList();

        foreach (var entity in entityList)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await _publisher.Publish(notification, cancellationToken);
        }
    }
}
