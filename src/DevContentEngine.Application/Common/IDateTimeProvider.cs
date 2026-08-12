namespace DevContentEngine.Application.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
