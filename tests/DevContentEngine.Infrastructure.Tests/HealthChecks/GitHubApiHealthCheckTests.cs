using DevContentEngine.Infrastructure.GitHub;
using DevContentEngine.Infrastructure.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DevContentEngine.Infrastructure.Tests.HealthChecks;

public class GitHubApiHealthCheckTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Stop();

    private GitHubApiHealthCheck CreateHealthCheck(string token = "fine-grained-test-token")
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var options = Options.Create(new GitHubOptions { Token = token });

        return new GitHubApiHealthCheck(httpClient, options);
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Healthy_when_the_API_accepts_the_token()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("""{"data":{"viewer":{"login":"nleceguic"}}}"""));

        var healthCheck = CreateHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Unhealthy_when_the_API_rejects_the_token()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(401).WithBody("""{"message":"Bad credentials"}"""));

        var healthCheck = CreateHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("401");
    }

    [Fact]
    public async Task CheckHealthAsync_returns_Unhealthy_without_calling_the_API_when_the_token_is_not_configured()
    {
        var healthCheck = CreateHealthCheck(token: "");

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
