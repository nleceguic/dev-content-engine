using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Events;

namespace DevContentEngine.Domain.Entities;

public sealed class ContentIdea : Entity
{
    private readonly List<Guid> _relatedActivityIds = [];

    public ContentOrigin Origin { get; }
    public decimal ActivityScore { get; }
    public IReadOnlyCollection<Guid> RelatedActivityIds => _relatedActivityIds.AsReadOnly();
    public Guid? RelatedTrendId { get; }
    public DateTime CreatedAt { get; }
    public ContentPath ChosenPath { get; }

    private ContentIdea()
    {
    }

    public ContentIdea(
        Guid id,
        ContentOrigin origin,
        decimal activityScore,
        IEnumerable<Guid> relatedActivityIds,
        Guid? relatedTrendId,
        DateTime createdAt,
        ContentPath chosenPath)
        : base(id)
    {
        if (!Enum.IsDefined(origin))
            throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown content origin.");

        if (!Enum.IsDefined(chosenPath))
            throw new ArgumentOutOfRangeException(nameof(chosenPath), chosenPath, "Unknown content path.");

        if (activityScore < 0)
            throw new ArgumentOutOfRangeException(nameof(activityScore), activityScore, "Activity score cannot be negative.");

        if (origin == ContentOrigin.Trend && relatedTrendId is null)
            throw new ArgumentException("An idea originated from a trend must reference that trend.", nameof(relatedTrendId));

        if (origin == ContentOrigin.GitHub && relatedTrendId is not null)
            throw new ArgumentException("An idea originated from GitHub activity cannot reference a trend.", nameof(relatedTrendId));

        Origin = origin;
        ActivityScore = activityScore;
        _relatedActivityIds = relatedActivityIds?.ToList() ?? [];
        RelatedTrendId = relatedTrendId;
        CreatedAt = createdAt;
        ChosenPath = chosenPath;

        AddDomainEvent(new ContentIdeaSelectedEvent(Id, Origin, ChosenPath));
    }
}
