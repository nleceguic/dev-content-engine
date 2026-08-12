using Cronos;
using DevContentEngine.Application.Common;

namespace DevContentEngine.Worker.Scheduling;

public sealed class DailyContentSchedule
{
    public const string CronExpressionText = "0 8 * * *";

    private static readonly TimeZoneInfo MadridTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private static readonly CronExpression Cron = CronExpression.Parse(CronExpressionText);

    private readonly IDateTimeProvider _dateTimeProvider;

    public DailyContentSchedule(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public DateTime GetNextOccurrenceUtc()
    {
        return Cron.GetNextOccurrence(_dateTimeProvider.UtcNow, MadridTimeZone)
            ?? throw new InvalidOperationException("The cron expression has no next occurrence.");
    }
}
