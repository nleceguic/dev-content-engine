namespace DevContentEngine.Domain.Services;

public sealed record RecentPostTopics
{
    public Guid PostId { get; }
    public IReadOnlyCollection<string> Keywords { get; }
    public DateTime PublishedAt { get; }

    public RecentPostTopics(Guid postId, IEnumerable<string> keywords, DateTime publishedAt)
    {
        if (postId == Guid.Empty)
            throw new ArgumentException("PostId cannot be empty.", nameof(postId));

        ArgumentNullException.ThrowIfNull(keywords);

        PostId = postId;
        Keywords = keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .ToList();
        PublishedAt = publishedAt;
    }
}
