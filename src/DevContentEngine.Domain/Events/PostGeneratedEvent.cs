using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Events;

public sealed record PostGeneratedEvent(Guid GeneratedPostId, Guid ContentIdeaId) : DomainEvent;
