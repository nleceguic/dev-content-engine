using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Entities;

public sealed class PublishedPost : Entity
{
    public Guid GeneratedPostId { get; }
    public DateTime PublishedAt { get; }
    public string? EngagementNotes { get; private set; }

    private PublishedPost()
    {
    }

    public PublishedPost(Guid id, Guid generatedPostId, DateTime publishedAt, string? engagementNotes = null)
        : base(id)
    {
        if (generatedPostId == Guid.Empty)
            throw new ArgumentException("GeneratedPostId cannot be empty.", nameof(generatedPostId));

        GeneratedPostId = generatedPostId;
        PublishedAt = publishedAt;
        EngagementNotes = string.IsNullOrWhiteSpace(engagementNotes) ? null : engagementNotes.Trim();
    }

    public void UpdateEngagementNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("Engagement notes cannot be empty.", nameof(notes));

        EngagementNotes = notes.Trim();
    }
}
