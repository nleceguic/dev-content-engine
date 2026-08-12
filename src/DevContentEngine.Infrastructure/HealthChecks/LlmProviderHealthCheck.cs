using DevContentEngine.Infrastructure.Llm;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.HealthChecks;

public sealed class LlmProviderHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;

    public LlmProviderHealthCheck(HttpClient httpClient, IOptions<LlmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return HealthCheckResult.Unhealthy("LLM API key is not configured (Llm__ApiKey).");
        }

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync("v1/models", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy($"Could not reach the LLM API: {ex.Message}", ex);
        }

        using (response)
        {
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("LLM API key is valid and the API is reachable.")
                : HealthCheckResult.Unhealthy(
                    $"LLM API returned {(int)response.StatusCode} ({response.StatusCode}). Check the configured API key.");
        }
    }
}
