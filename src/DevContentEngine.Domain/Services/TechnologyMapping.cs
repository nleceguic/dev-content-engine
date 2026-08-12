namespace DevContentEngine.Domain.Services;

public sealed record TechnologyMapping
{
    public string Technology { get; }
    public IReadOnlyCollection<string> FileExtensions { get; }
    public IReadOnlyCollection<string> PathKeywords { get; }
    public IReadOnlyCollection<string> MessageKeywords { get; }

    public TechnologyMapping(
        string technology,
        IReadOnlyCollection<string>? fileExtensions = null,
        IReadOnlyCollection<string>? pathKeywords = null,
        IReadOnlyCollection<string>? messageKeywords = null)
    {
        if (string.IsNullOrWhiteSpace(technology))
            throw new ArgumentException("Technology name cannot be empty.", nameof(technology));

        FileExtensions = fileExtensions ?? [];
        PathKeywords = pathKeywords ?? [];
        MessageKeywords = messageKeywords ?? [];

        if (FileExtensions.Count == 0 && PathKeywords.Count == 0 && MessageKeywords.Count == 0)
            throw new ArgumentException("A technology mapping must define at least one signal to match on.", nameof(fileExtensions));

        Technology = technology.Trim();
    }
}
