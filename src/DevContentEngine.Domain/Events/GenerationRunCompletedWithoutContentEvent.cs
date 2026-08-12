using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Events;

public sealed record GenerationRunCompletedWithoutContentEvent(Guid GenerationRunId, string? Reason) : DomainEvent;
