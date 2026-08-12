using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Events;

public sealed record GenerationRunFailedEvent(Guid GenerationRunId, string ErrorMessage) : DomainEvent;
