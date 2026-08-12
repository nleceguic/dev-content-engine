using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Application.UseCases.GenerationRuns;

public sealed record GenerationRunDto(
    Guid Id,
    DateTime StartedAt,
    DateTime? FinishedAt,
    TimeSpan? Duration,
    GenerationRunStatus? Status,
    ContentPath? ChosenPath,
    int? TokensUsed,
    string? ErrorMessage,
    Guid? ResultingPostId)
{
    public static GenerationRunDto FromEntity(GenerationRun run)
    {
        var duration = run.FinishedAt is { } finishedAt ? finishedAt - run.StartedAt : (TimeSpan?)null;

        return new GenerationRunDto(
            run.Id,
            run.StartedAt,
            run.FinishedAt,
            duration,
            run.Status,
            run.ChosenPath,
            run.TokensUsed,
            run.ErrorMessage,
            run.ResultingPostId);
    }
}
