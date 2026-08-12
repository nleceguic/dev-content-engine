using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Events;

public sealed record PostValidationFailedEvent(Guid GeneratedPostId, string Reason) : DomainEvent;
