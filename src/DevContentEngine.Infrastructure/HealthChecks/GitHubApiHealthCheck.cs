using System.Net.Http.Json;
using DevContentEngine.Infrastructure.GitHub;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.HealthChecks;

public sealed class GitHubApiHealthCheck : IHealthCheck
{
    private const string ViewerQuery = "{ viewer { login } }";

    private readonly HttpClient _httpClient;
    private readonly GitHubOptions _options;

    public GitHubApiHealthCheck(HttpClient httpClient, IOptions<GitHubOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            return HealthCheckResult.Unhealthy("GitHub token is not configured (GitHub__Token).");
        }

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("graphql", new { query = ViewerQuery }, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy($"Could not reach the GitHub API: {ex.Message}", ex);
        }

        using (response)
        {
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("GitHub token is valid and the API is reachable.")
                : HealthCheckResult.Unhealthy(
                    $"GitHub API returned {(int)response.StatusCode} ({response.StatusCode}). Check the configured token.");
        }
    }
}
