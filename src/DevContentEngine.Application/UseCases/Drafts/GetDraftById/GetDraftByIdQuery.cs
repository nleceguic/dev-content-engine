using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.GetDraftById;

public sealed record GetDraftByIdQuery(Guid DraftId) : IRequest<GeneratedPostDto>;
