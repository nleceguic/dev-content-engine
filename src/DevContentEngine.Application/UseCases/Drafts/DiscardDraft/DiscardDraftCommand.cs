using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.DiscardDraft;

public sealed record DiscardDraftCommand(Guid DraftId) : IRequest<GeneratedPostDto>;
