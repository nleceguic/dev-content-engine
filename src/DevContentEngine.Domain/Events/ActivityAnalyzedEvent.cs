using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Events;

public sealed record ActivityAnalyzedEvent(Guid ActivityId, Guid RepositoryId, bool IsNoise) : DomainEvent;
