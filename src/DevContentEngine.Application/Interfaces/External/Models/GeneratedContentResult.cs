namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GeneratedContentResult(
    string Hook,
    string Body,
    string Conclusion,
    string? Cta,
    IReadOnlyCollection<string> Hashtags,
    IReadOnlyCollection<string> Sources,
    Guid PromptVersionId);
