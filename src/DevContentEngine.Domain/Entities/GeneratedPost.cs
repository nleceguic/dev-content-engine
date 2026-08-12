using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Events;

namespace DevContentEngine.Domain.Entities;

public sealed class GeneratedPost : Entity
{
    private readonly List<string> _hashtags = [];
    private readonly List<string> _sources = [];

    public Guid ContentIdeaId { get; }
    public string Hook { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Conclusion { get; private set; } = string.Empty;
    public string? Cta { get; private set; }
    public IReadOnlyCollection<string> Hashtags => _hashtags.AsReadOnly();
    public IReadOnlyCollection<string> Sources => _sources.AsReadOnly();
    public GeneratedPostStatus Status { get; private set; }
    public ContentOrigin Origin { get; }
    public Guid PromptVersionId { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private GeneratedPost()
    {
    }

    public GeneratedPost(
        Guid id,
        Guid contentIdeaId,
        string hook,
        string body,
        string conclusion,
        string? cta,
        IEnumerable<string> hashtags,
        IEnumerable<string> sources,
        ContentOrigin origin,
        Guid promptVersionId,
        DateTime createdAt)
        : base(id)
    {
        if (contentIdeaId == Guid.Empty)
            throw new ArgumentException("ContentIdeaId cannot be empty.", nameof(contentIdeaId));

        if (string.IsNullOrWhiteSpace(hook))
            throw new ArgumentException("Hook cannot be empty.", nameof(hook));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty.", nameof(body));

        if (string.IsNullOrWhiteSpace(conclusion))
            throw new ArgumentException("Conclusion cannot be empty.", nameof(conclusion));

        if (!Enum.IsDefined(origin))
            throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown content origin.");

        if (promptVersionId == Guid.Empty)
            throw new ArgumentException("PromptVersionId cannot be empty.", nameof(promptVersionId));

        var sourceList = sources?.Where(source => !string.IsNullOrWhiteSpace(source)).ToList() ?? [];

        if (sourceList.Count == 0)
            throw new ArgumentException("A generated post must be traceable to at least one real source.", nameof(sources));

        ContentIdeaId = contentIdeaId;
        Hook = hook.Trim();
        Body = body.Trim();
        Conclusion = conclusion.Trim();
        Cta = string.IsNullOrWhiteSpace(cta) ? null : cta.Trim();
        _hashtags = hashtags?.Where(hashtag => !string.IsNullOrWhiteSpace(hashtag)).ToList() ?? [];
        _sources = sourceList;
        Status = GeneratedPostStatus.Draft;
        Origin = origin;
        PromptVersionId = promptVersionId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        AddDomainEvent(new PostGeneratedEvent(Id, ContentIdeaId));
        AddDomainEvent(new DraftReadyEvent(Id));
    }

    public void Edit(string hook, string body, string conclusion, string? cta, IEnumerable<string> hashtags, DateTime editedAt)
    {
        EnsureNotInTerminalState();

        if (string.IsNullOrWhiteSpace(hook))
            throw new ArgumentException("Hook cannot be empty.", nameof(hook));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty.", nameof(body));

        if (string.IsNullOrWhiteSpace(conclusion))
            throw new ArgumentException("Conclusion cannot be empty.", nameof(conclusion));

        Hook = hook.Trim();
        Body = body.Trim();
        Conclusion = conclusion.Trim();
        Cta = string.IsNullOrWhiteSpace(cta) ? null : cta.Trim();
        _hashtags.Clear();
        _hashtags.AddRange(hashtags?.Where(hashtag => !string.IsNullOrWhiteSpace(hashtag)) ?? []);
        Status = GeneratedPostStatus.Edited;
        UpdatedAt = editedAt;
    }

    public void MarkAsUsed(DateTime usedAt)
    {
        EnsureNotInTerminalState();

        Status = GeneratedPostStatus.Used;
        UpdatedAt = usedAt;
    }

    public void Discard(DateTime discardedAt)
    {
        if (Status == GeneratedPostStatus.Used)
            throw new InvalidOperationException("A post that was already used cannot be discarded.");

        Status = GeneratedPostStatus.Discarded;
        UpdatedAt = discardedAt;
    }

    public void FailValidation(string reason, DateTime failedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A validation failure requires a reason.", nameof(reason));

        if (Status == GeneratedPostStatus.Used)
            throw new InvalidOperationException("A post that was already used cannot fail validation.");

        Status = GeneratedPostStatus.Discarded;
        UpdatedAt = failedAt;

        AddDomainEvent(new PostValidationFailedEvent(Id, reason.Trim()));
    }

    private void EnsureNotInTerminalState()
    {
        if (Status is GeneratedPostStatus.Used or GeneratedPostStatus.Discarded)
            throw new InvalidOperationException($"Cannot modify a post in '{Status}' status.");
    }
}
