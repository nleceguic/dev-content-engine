using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Domain.Entities;

public sealed class PromptVersion : Entity
{
    public PromptRole Role { get; }
    public string Content { get; } = string.Empty;
    public DateTime CreatedAt { get; }
    public bool IsActive { get; private set; }

    private PromptVersion()
    {
    }

    public PromptVersion(Guid id, PromptRole role, string content, DateTime createdAt)
        : base(id)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown prompt role.");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Prompt content cannot be empty.", nameof(content));

        Role = role;
        Content = content.Trim();
        CreatedAt = createdAt;
        IsActive = true;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
