namespace DevContentEngine.Domain.Services;

public sealed record GroupedCommitActivity
{
    public Guid RepositoryId { get; }
    public DateOnly Date { get; }
    public IReadOnlyCollection<string> CommitExternalIds { get; }
    public IReadOnlyCollection<string> Messages { get; }
    public int TotalFilesChanged { get; }
    public int TotalLinesChanged { get; }
    public DateTime LatestTimestamp { get; }

    public int CommitCount => CommitExternalIds.Count;

    public GroupedCommitActivity(
        Guid repositoryId,
        DateOnly date,
        IReadOnlyCollection<string> commitExternalIds,
        IReadOnlyCollection<string> messages,
        int totalFilesChanged,
        int totalLinesChanged,
        DateTime latestTimestamp)
    {
        if (repositoryId == Guid.Empty)
            throw new ArgumentException("RepositoryId cannot be empty.", nameof(repositoryId));

        if (commitExternalIds is null || commitExternalIds.Count == 0)
            throw new ArgumentException("A grouped activity must reference at least one commit.", nameof(commitExternalIds));

        if (totalFilesChanged < 0)
            throw new ArgumentOutOfRangeException(nameof(totalFilesChanged), totalFilesChanged, "TotalFilesChanged cannot be negative.");

        if (totalLinesChanged < 0)
            throw new ArgumentOutOfRangeException(nameof(totalLinesChanged), totalLinesChanged, "TotalLinesChanged cannot be negative.");

        RepositoryId = repositoryId;
        Date = date;
        CommitExternalIds = commitExternalIds;
        Messages = messages ?? [];
        TotalFilesChanged = totalFilesChanged;
        TotalLinesChanged = totalLinesChanged;
        LatestTimestamp = latestTimestamp;
    }
}
