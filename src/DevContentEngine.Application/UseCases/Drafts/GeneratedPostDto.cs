using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Application.UseCases.Drafts;

public sealed record GeneratedPostDto(
    Guid Id,
    Guid ContentIdeaId,
    string Hook,
    string Body,
    string Conclusion,
    string? Cta,
    IReadOnlyCollection<string> Hashtags,
    IReadOnlyCollection<string> Sources,
    GeneratedPostStatus Status,
    ContentOrigin Origin,
    Guid PromptVersionId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static GeneratedPostDto FromEntity(GeneratedPost post)
    {
        return new GeneratedPostDto(
            post.Id,
            post.ContentIdeaId,
            post.Hook,
            post.Body,
            post.Conclusion,
            post.Cta,
            post.Hashtags,
            post.Sources,
            post.Status,
            post.Origin,
            post.PromptVersionId,
            post.CreatedAt,
            post.UpdatedAt);
    }
}
