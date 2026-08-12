namespace DevContentEngine.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
