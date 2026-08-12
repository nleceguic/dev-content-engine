using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Application.UseCases.GenerateDailyContent;

public sealed record GenerateDailyContentResult(
    GenerationRunStatus Status,
    Guid GenerationRunId,
    Guid? GeneratedPostId,
    string? ErrorMessage);
