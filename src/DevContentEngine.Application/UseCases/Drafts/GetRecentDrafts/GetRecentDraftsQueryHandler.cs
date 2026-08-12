using DevContentEngine.Application.Interfaces.Persistence;
using MediatR;

namespace DevContentEngine.Application.UseCases.Drafts.GetRecentDrafts;

public sealed class GetRecentDraftsQueryHandler : IRequestHandler<GetRecentDraftsQuery, IReadOnlyCollection<GeneratedPostDto>>
{
    private readonly IGeneratedPostRepository _repository;

    public GetRecentDraftsQueryHandler(IGeneratedPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<GeneratedPostDto>> Handle(GetRecentDraftsQuery request, CancellationToken cancellationToken)
    {
        var drafts = await _repository.GetLatestAsync(request.Count, cancellationToken);

        return drafts.Select(GeneratedPostDto.FromEntity).ToList();
    }
}
