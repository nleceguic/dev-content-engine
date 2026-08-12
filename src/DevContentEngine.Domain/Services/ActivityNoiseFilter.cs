namespace DevContentEngine.Domain.Services;

public sealed class ActivityNoiseFilter
{
    public const double DefaultFormatOnlyLinesPerFileThreshold = 150.0;

    private readonly IReadOnlyCollection<string> _noiseMessagePatterns;
    private readonly double _formatOnlyLinesPerFileThreshold;

    public ActivityNoiseFilter(
        IEnumerable<string>? noiseMessagePatterns = null,
        double formatOnlyLinesPerFileThreshold = DefaultFormatOnlyLinesPerFileThreshold)
    {
        if (formatOnlyLinesPerFileThreshold <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(formatOnlyLinesPerFileThreshold),
                formatOnlyLinesPerFileThreshold,
                "Threshold must be positive.");

        _noiseMessagePatterns = (noiseMessagePatterns ?? DefaultNoiseMessagePatterns.Patterns)
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .ToList();
        _formatOnlyLinesPerFileThreshold = formatOnlyLinesPerFileThreshold;
    }

    public bool IsNoise(CommitInfo commit)
    {
        ArgumentNullException.ThrowIfNull(commit);

        return MatchesNoiseMessagePattern(commit.Message) || IsFormatOnlyChange(commit.FilesChanged, commit.LinesChanged);
    }

    public IReadOnlyCollection<CommitInfo> ExcludeNoise(IEnumerable<CommitInfo> commits)
    {
        ArgumentNullException.ThrowIfNull(commits);

        return commits.Where(commit => !IsNoise(commit)).ToList();
    }

    public IReadOnlyCollection<GroupedCommitActivity> GroupByDayAndRepository(IEnumerable<CommitInfo> commits)
    {
        ArgumentNullException.ThrowIfNull(commits);

        return commits
            .GroupBy(commit => (commit.RepositoryId, Date: DateOnly.FromDateTime(commit.Timestamp)))
            .Select(group => new GroupedCommitActivity(
                repositoryId: group.Key.RepositoryId,
                date: group.Key.Date,
                commitExternalIds: group.Select(commit => commit.ExternalId).ToList(),
                messages: group.Select(commit => commit.Message).ToList(),
                totalFilesChanged: group.Sum(commit => commit.FilesChanged),
                totalLinesChanged: group.Sum(commit => commit.LinesChanged),
                latestTimestamp: group.Max(commit => commit.Timestamp)))
            .ToList();
    }

    public IReadOnlyCollection<GroupedCommitActivity> FilterAndGroup(IEnumerable<CommitInfo> commits)
    {
        ArgumentNullException.ThrowIfNull(commits);

        return GroupByDayAndRepository(ExcludeNoise(commits));
    }

    private bool MatchesNoiseMessagePattern(string message)
    {
        return _noiseMessagePatterns.Any(pattern => message.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsFormatOnlyChange(int filesChanged, int linesChanged)
    {
        if (filesChanged <= 0)
            return false;

        var linesPerFile = (double)linesChanged / filesChanged;

        return linesPerFile >= _formatOnlyLinesPerFileThreshold;
    }
}
