using MediatR;

namespace DevContentEngine.Application.UseCases.GenerationRuns.GetRecentGenerationRuns;

public sealed record GetRecentGenerationRunsQuery(int Count) : IRequest<IReadOnlyCollection<GenerationRunDto>>;
