using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.ApproveDraft;

public sealed record ApproveDraftCommand(Guid DraftId) : IRequest<GeneratedPostDto>;
