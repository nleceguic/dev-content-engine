using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Entities;

public sealed class Trend : Entity
{
    public string Title { get; } = string.Empty;
    public string Source { get; } = string.Empty;
    public string Url { get; } = string.Empty;
    public DateTime PublishedAt { get; }
    public decimal RelevanceScore { get; }

    private Trend()
    {
    }

    public Trend(
        Guid id,
        string title,
        string source,
        string url,
        DateTime publishedAt,
        decimal relevanceScore)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty.", nameof(source));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty.", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("Url must be a well-formed absolute URI.", nameof(url));

        if (relevanceScore < 0)
            throw new ArgumentOutOfRangeException(nameof(relevanceScore), relevanceScore, "Relevance score cannot be negative.");

        Title = title.Trim();
        Source = source.Trim();
        Url = url.Trim();
        PublishedAt = publishedAt;
        RelevanceScore = relevanceScore;
    }
}
