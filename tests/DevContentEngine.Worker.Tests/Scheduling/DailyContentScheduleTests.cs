using DevContentEngine.Application.Common;
using DevContentEngine.Worker.Scheduling;
using FluentAssertions;
using Moq;

namespace DevContentEngine.Worker.Tests.Scheduling;

public class DailyContentScheduleTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private DailyContentSchedule CreateSchedule(DateTime simulatedUtcNow)
    {
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(simulatedUtcNow);

        return new DailyContentSchedule(_dateTimeProvider.Object);
    }

    [Fact]
    public void GetNextOccurrenceUtc_in_winter_returns_08_00_CET_which_is_07_00_UTC()
    {
        var schedule = CreateSchedule(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        schedule.GetNextOccurrenceUtc().Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetNextOccurrenceUtc_in_summer_returns_08_00_CEST_which_is_06_00_UTC()
    {
        var schedule = CreateSchedule(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));

        schedule.GetNextOccurrenceUtc().Should().Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetNextOccurrenceUtc_on_the_spring_forward_transition_day_already_lands_in_CEST()
    {
        var schedule = CreateSchedule(new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc));

        schedule.GetNextOccurrenceUtc().Should().Be(new DateTime(2026, 3, 29, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetNextOccurrenceUtc_on_the_fall_back_transition_day_already_lands_in_CET()
    {
        var schedule = CreateSchedule(new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc));

        schedule.GetNextOccurrenceUtc().Should().Be(new DateTime(2026, 10, 25, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetNextOccurrenceUtc_is_strictly_after_the_clocks_current_time_even_at_exactly_08_00_local()
    {
        var schedule = CreateSchedule(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));

        schedule.GetNextOccurrenceUtc().Should().Be(new DateTime(2026, 1, 16, 7, 0, 0, DateTimeKind.Utc));
    }
}
