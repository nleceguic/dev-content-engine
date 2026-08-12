using DevContentEngine.Application.Interfaces.External.Models;

namespace DevContentEngine.Application.Interfaces.External;

public interface IGitHubClient
{
    Task<GitHubUserActivity> GetUserActivityAsync(
        string username,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
}
