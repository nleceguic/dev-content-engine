using DevContentEngine.Domain.Common;

namespace DevContentEngine.Domain.Entities;

public sealed class GitHubRepository : Entity
{
    public string Owner { get; } = string.Empty;
    public string Name { get; } = string.Empty;
    public DateTime DetectedAt { get; }
    public bool IsActive { get; private set; }

    public string FullName => $"{Owner}/{Name}";

    private GitHubRepository()
    {
    }

    public GitHubRepository(Guid id, string owner, string name, DateTime detectedAt)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Repository owner cannot be empty.", nameof(owner));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name cannot be empty.", nameof(name));

        Owner = owner.Trim();
        Name = name.Trim();
        DetectedAt = detectedAt;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}
