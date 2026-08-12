using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.EditDraft;

public sealed record EditDraftCommand(
    Guid DraftId,
    string Hook,
    string Body,
    string Conclusion,
    string? Cta,
    IReadOnlyCollection<string> Hashtags) : IRequest<GeneratedPostDto>;
