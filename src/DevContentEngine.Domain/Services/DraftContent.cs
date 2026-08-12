namespace DevContentEngine.Domain.Services;

public sealed record DraftContent(
    string Hook,
    string Body,
    string Conclusion,
    string? Cta,
    IReadOnlyCollection<string> Hashtags,
    IReadOnlyCollection<string> Sources,
    string Diagrama);
