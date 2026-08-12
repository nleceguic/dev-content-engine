using MediatR;

namespace DevContentEngine.Application.UseCases.GenerateDailyContent;

public sealed record GenerateDailyContentCommand(string GitHubUsername) : IRequest<GenerateDailyContentResult>;
