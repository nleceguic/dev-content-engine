using DevContentEngine.Infrastructure.HealthChecks;
using DevContentEngine.Infrastructure.Llm;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DevContentEngine.Infrastructure.Tests.HealthChecks;

public class LlmProviderHealthCheckTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Stop();

    private LlmProviderHealthCheck CreateHealthCheck(string apiKey = "test-api-key")
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var options = Options.Create(new LlmOptions { ApiKey = apiKey });

        return new LlmProviderHealthCheck(httpClient, options);
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Healthy_when_the_API_accepts_the_key()
    {
        _server
            .Given(Request.Create().WithPath("/v1/models").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"data":[]}"""));

        var healthCheck = CreateHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Unhealthy_when_the_API_rejects_the_key()
    {
        _server
            .Given(Request.Create().WithPath("/v1/models").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(401).WithBody("""{"error":"invalid x-api-key"}"""));

        var healthCheck = CreateHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("401");
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Unhealthy_without_calling_the_API_when_the_key_is_not_configured()
    {
        var healthCheck = CreateHealthCheck(apiKey: "");

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not configured");
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Unhealthy_when_the_API_is_unreachable()
    {
        _server.Stop();

        var healthCheck = CreateHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
