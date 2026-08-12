namespace DevContentEngine.Domain.Services;

public sealed class TopicRepetitionOptions
{
    public const double DefaultOverlapThreshold = 0.6;
    public const decimal DefaultPenalty = 3.0m;

    public static readonly TimeSpan DefaultLookbackWindow = TimeSpan.FromDays(14);

    public double OverlapThreshold { get; }
    public decimal Penalty { get; }
    public TimeSpan LookbackWindow { get; }

    public TopicRepetitionOptions(
        double overlapThreshold = DefaultOverlapThreshold,
        decimal penalty = DefaultPenalty,
        TimeSpan? lookbackWindow = null)
    {
        if (overlapThreshold is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(
                nameof(overlapThreshold),
                overlapThreshold,
                "Overlap threshold must be a ratio greater than 0 and less than or equal to 1.");

        if (penalty < 0)
            throw new ArgumentOutOfRangeException(nameof(penalty), penalty, "Penalty must be a non-negative magnitude.");

        var window = lookbackWindow ?? DefaultLookbackWindow;

        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lookbackWindow), lookbackWindow, "Lookback window must be positive.");

        OverlapThreshold = overlapThreshold;
        Penalty = penalty;
        LookbackWindow = window;
    }

    public static TopicRepetitionOptions Default { get; } = new();
}
