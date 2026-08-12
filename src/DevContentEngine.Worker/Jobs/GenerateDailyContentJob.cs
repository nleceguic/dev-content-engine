using DevContentEngine.Application.UseCases.GenerateDailyContent;
using DevContentEngine.Infrastructure.GitHub;
using MediatR;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Worker.Jobs;

public sealed class GenerateDailyContentJob
{
    private readonly ISender _sender;
    private readonly GitHubOptions _gitHubOptions;
    private readonly ILogger<GenerateDailyContentJob> _logger;

    public GenerateDailyContentJob(ISender sender, IOptions<GitHubOptions> gitHubOptions, ILogger<GenerateDailyContentJob> logger)
    {
        _sender = sender;
        _gitHubOptions = gitHubOptions.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_gitHubOptions.Username))
        {
            throw new InvalidOperationException(
                "GitHub username is not configured. Set the GitHub__Username environment variable.");
        }

        var result = await _sender.Send(new GenerateDailyContentCommand(_gitHubOptions.Username), cancellationToken);

        _logger.LogInformation(
            "Daily content generation run {GenerationRunId} finished with status {Status}.",
            result.GenerationRunId,
            result.Status);
    }
}
