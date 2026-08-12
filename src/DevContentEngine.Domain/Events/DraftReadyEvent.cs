using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Events;

public sealed record DraftReadyEvent(Guid GeneratedPostId) : DomainEvent;
