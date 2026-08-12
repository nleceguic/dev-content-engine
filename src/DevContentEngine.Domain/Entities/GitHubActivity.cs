using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Events;

namespace DevContentEngine.Domain.Entities;

public sealed class GitHubActivity : Entity
{
    private readonly List<string> _detectedTechnologies = [];

    public Guid RepositoryId { get; }
    public GitHubActivityType Type { get; }
    public IReadOnlyCollection<string> DetectedTechnologies => _detectedTechnologies.AsReadOnly();
    public string Summary { get; } = string.Empty;
    public string ExternalId { get; } = string.Empty;
    public DateTime Timestamp { get; }
    public bool IsNoise { get; private set; }

    private GitHubActivity()
    {
    }

    public GitHubActivity(
        Guid id,
        Guid repositoryId,
        GitHubActivityType type,
        IEnumerable<string> detectedTechnologies,
        string summary,
        string externalId,
        DateTime timestamp)
        : base(id)
    {
        if (repositoryId == Guid.Empty)
            throw new ArgumentException("RepositoryId cannot be empty.", nameof(repositoryId));

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown GitHub activity type.");

        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Summary cannot be empty.", nameof(summary));

        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId cannot be empty.", nameof(externalId));

        RepositoryId = repositoryId;
        Type = type;
        _detectedTechnologies = detectedTechnologies?
            .Where(technology => !string.IsNullOrWhiteSpace(technology))
            .Select(technology => technology.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        Summary = summary.Trim();
        ExternalId = externalId.Trim();
        Timestamp = timestamp;
        IsNoise = false;
    }

    public void MarkAnalyzed(bool isNoise)
    {
        IsNoise = isNoise;

        AddDomainEvent(new ActivityAnalyzedEvent(Id, RepositoryId, IsNoise));
    }
}
