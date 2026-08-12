using DevContentEngine.Application.Common;

namespace DevContentEngine.Infrastructure.Tests.TestHelpers;

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}
