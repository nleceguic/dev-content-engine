using DevContentEngine.Application.Interfaces.Persistence;
using MediatR;

namespace DevContentEngine.Application.UseCases.GenerationRuns.GetRecentGenerationRuns;

public sealed class GetRecentGenerationRunsQueryHandler
    : IRequestHandler<GetRecentGenerationRunsQuery, IReadOnlyCollection<GenerationRunDto>>
{
    private readonly IGenerationRunRepository _repository;

    public GetRecentGenerationRunsQueryHandler(IGenerationRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<GenerationRunDto>> Handle(GetRecentGenerationRunsQuery request, CancellationToken cancellationToken)
    {
        var runs = await _repository.GetLatestAsync(request.Count, cancellationToken);

        return runs.Select(GenerationRunDto.FromEntity).ToList();
    }
}
