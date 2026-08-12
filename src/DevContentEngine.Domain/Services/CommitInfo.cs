namespace DevContentEngine.Domain.Services;

public sealed record CommitInfo
{
    public Guid RepositoryId { get; }
    public string ExternalId { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }
    public int FilesChanged { get; }
    public int LinesChanged { get; }

    public CommitInfo(
        Guid repositoryId,
        string externalId,
        string message,
        DateTime timestamp,
        int filesChanged,
        int linesChanged)
    {
        if (repositoryId == Guid.Empty)
            throw new ArgumentException("RepositoryId cannot be empty.", nameof(repositoryId));

        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId cannot be empty.", nameof(externalId));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        if (filesChanged < 0)
            throw new ArgumentOutOfRangeException(nameof(filesChanged), filesChanged, "FilesChanged cannot be negative.");

        if (linesChanged < 0)
            throw new ArgumentOutOfRangeException(nameof(linesChanged), linesChanged, "LinesChanged cannot be negative.");

        RepositoryId = repositoryId;
        ExternalId = externalId.Trim();
        Message = message.Trim();
        Timestamp = timestamp;
        FilesChanged = filesChanged;
        LinesChanged = linesChanged;
    }
}
