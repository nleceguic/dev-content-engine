using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.GetRecentDrafts;

public sealed record GetRecentDraftsQuery(int Count) : IRequest<IReadOnlyCollection<GeneratedPostDto>>;
