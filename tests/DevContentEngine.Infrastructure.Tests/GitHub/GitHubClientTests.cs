using DevContentEngine.Infrastructure.GitHub;
using DevContentEngine.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DevContentEngine.Infrastructure.Tests.GitHub;

public class GitHubClientTests : IDisposable
{
    private static readonly DateTime SinceUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly InMemoryLogger<GitHubClient> _logger = new();

    public void Dispose() => _server.Stop();

    private GitHubClient CreateClient(double rateLimitWarningThreshold = 0.1)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var options = Options.Create(new GitHubOptions
        {
            Token = "fine-grained-test-token",
            RateLimitWarningThreshold = rateLimitWarningThreshold
        });

        return new GitHubClient(httpClient, options, _logger);
    }

    private const string SuccessfulResponseBody = """
        {
          "data": {
            "user": {
              "repositories": {
                "nodes": [
                  {
                    "name": "dev-content-engine",
                    "owner": { "login": "nleceguic" },
                    "createdAt": "2025-01-01T00:00:00Z",
                    "defaultBranchRef": {
                      "target": {
                        "history": {
                          "nodes": [
                            {
                              "oid": "abc123",
                              "message": "Implement pipeline",
                              "committedDate": "2026-08-05T10:00:00Z",
                              "additions": 50,
                              "deletions": 10,
                              "changedFilesIfAvailable": 3
                            }
                          ]
                        }
                      }
                    },
                    "pullRequests": {
                      "nodes": [
                        {
                          "number": 42,
                          "title": "Add GitHub client",
                          "mergedAt": "2026-08-06T12:00:00Z",
                          "files": {
                            "nodes": [
                              { "path": "src/GitHubClient.cs" },
                              { "path": "src/GitHubOptions.cs" }
                            ]
                          }
                        }
                      ]
                    },
                    "issues": {
                      "nodes": [
                        {
                          "number": 7,
                          "title": "Fix rate limit bug",
                          "closedAt": "2026-08-07T09:00:00Z",
                          "comments": {
                            "nodes": [
                              { "body": "Fixed in latest commit" }
                            ]
                          }
                        }
                      ]
                    }
                  },
                  {
                    "name": "new-repo",
                    "owner": { "login": "nleceguic" },
                    "createdAt": "2026-08-08T00:00:00Z",
                    "defaultBranchRef": null,
                    "pullRequests": { "nodes": [] },
                    "issues": { "nodes": [] }
                  }
                ]
              }
            }
          }
        }
        """;

    [Fact]
    public async Task GetUserActivityAsync_parses_commits_pull_requests_issues_and_new_repositories_from_a_successful_response()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-RateLimit-Limit", "5000")
                .WithHeader("X-RateLimit-Remaining", "4999")
                .WithBody(SuccessfulResponseBody));

        var client = CreateClient();

        var result = await client.GetUserActivityAsync("nleceguic", SinceUtc);

        result.NewRepositories.Should().ContainSingle();
        result.NewRepositories.Single().Name.Should().Be("new-repo");
        result.NewRepositories.Single().Owner.Should().Be("nleceguic");

        result.Commits.Should().ContainSingle();
        var commit = result.Commits.Single();
        commit.Sha.Should().Be("abc123");
        commit.Message.Should().Be("Implement pipeline");
        commit.RepositoryName.Should().Be("dev-content-engine");
        commit.FilesChanged.Should().Be(3);
        commit.LinesChanged.Should().Be(60);

        result.PullRequests.Should().ContainSingle();
        var pullRequest = result.PullRequests.Single();
        pullRequest.ExternalId.Should().Be("42");
        pullRequest.Title.Should().Be("Add GitHub client");
        pullRequest.ChangedFilePaths.Should().BeEquivalentTo(["src/GitHubClient.cs", "src/GitHubOptions.cs"]);

        result.Issues.Should().ContainSingle();
        var issue = result.Issues.Single();
        issue.ExternalId.Should().Be("7");
        issue.Title.Should().Be("Fix rate limit bug");
        issue.ClosingContext.Should().Be("Fixed in latest commit");
    }

    [Fact]
    public async Task GetUserActivityAsync_does_not_log_a_warning_when_the_rate_limit_is_healthy()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-RateLimit-Limit", "5000")
                .WithHeader("X-RateLimit-Remaining", "4999")
                .WithBody(SuccessfulResponseBody));

        var client = CreateClient();

        await client.GetUserActivityAsync("nleceguic", SinceUtc);

        _logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task GetUserActivityAsync_logs_a_warning_instead_of_throwing_when_the_rate_limit_is_nearly_exhausted()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-RateLimit-Limit", "5000")
                .WithHeader("X-RateLimit-Remaining", "50")
                .WithBody(SuccessfulResponseBody));

        var client = CreateClient(rateLimitWarningThreshold: 0.1);

        var result = await client.GetUserActivityAsync("nleceguic", SinceUtc);

        result.Should().NotBeNull();
        _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning);
        _logger.Entries.Single(entry => entry.Level == LogLevel.Warning).Message
            .Should().Contain("50").And.Contain("5000");
    }

    [Fact]
    public async Task GetUserActivityAsync_throws_GitHubIntegrationException_when_the_token_is_invalid()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"message": "Bad credentials"}"""));

        var client = CreateClient();

        var act = async () => await client.GetUserActivityAsync("nleceguic", SinceUtc);

        var exception = await act.Should().ThrowAsync<GitHubIntegrationException>();
        exception.Which.Message.Should().Contain("401");
    }

    [Fact]
    public async Task GetUserActivityAsync_throws_GitHubIntegrationException_on_a_server_error()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody("Service Unavailable"));

        var client = CreateClient();

        var act = async () => await client.GetUserActivityAsync("nleceguic", SinceUtc);

        var exception = await act.Should().ThrowAsync<GitHubIntegrationException>();
        exception.Which.Message.Should().Contain("503");
    }

    [Fact]
    public async Task GetUserActivityAsync_throws_GitHubIntegrationException_when_the_response_contains_graphql_errors()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data": null, "errors": [{"message": "Could not resolve to a User with the login of 'nleceguic'."}]}"""));

        var client = CreateClient();

        var act = async () => await client.GetUserActivityAsync("nleceguic", SinceUtc);

        var exception = await act.Should().ThrowAsync<GitHubIntegrationException>();
        exception.Which.Message.Should().Contain("Could not resolve to a User");
    }

    private const string RepositoryDetailResponseBody = """
        {
          "data": {
            "repository": {
              "description": "SaaS de gestion de pedidos para restaurantes.",
              "primaryLanguage": { "name": "C#" },
              "repositoryTopics": {
                "nodes": [
                  { "topic": { "name": "clean-architecture" } },
                  { "topic": { "name": "dotnet" } }
                ]
              },
              "readme": { "text": "# Rush Order\n\nClean Architecture, .NET 9 y CQRS." },
              "readmeLower": null,
              "rootTree": {
                "entries": [
                  { "name": "src", "type": "tree" },
                  { "name": "tests", "type": "tree" },
                  { "name": "README.md", "type": "blob" }
                ]
              }
            }
          }
        }
        """;

    [Fact]
    public async Task GetRepositoryDetailAsync_parses_description_language_topics_readme_and_top_level_folders()
    {
        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-RateLimit-Limit", "5000")
                .WithHeader("X-RateLimit-Remaining", "4999")
                .WithBody(RepositoryDetailResponseBody));

        var client = CreateClient();

        var result = await client.GetRepositoryDetailAsync("nleceguic", "rush-order");

        result.Owner.Should().Be("nleceguic");
        result.Name.Should().Be("rush-order");
        result.Description.Should().Be("SaaS de gestion de pedidos para restaurantes.");
        result.PrimaryLanguage.Should().Be("C#");
        result.Topics.Should().BeEquivalentTo(["clean-architecture", "dotnet"]);
        result.ReadmeExcerpt.Should().Be("# Rush Order\n\nClean Architecture, .NET 9 y CQRS.");
        result.TopLevelFolders.Should().BeEquivalentTo(["src", "tests"]);
    }

    [Fact]
    public async Task GetRepositoryDetailAsync_falls_back_to_lowercase_readme_when_README_md_is_missing()
    {
        const string body = """
            {
              "data": {
                "repository": {
                  "description": null,
                  "primaryLanguage": null,
                  "repositoryTopics": { "nodes": [] },
                  "readme": null,
                  "readmeLower": { "text": "lowercase readme content" },
                  "rootTree": { "entries": [] }
                }
              }
            }
            """;

        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

        var client = CreateClient();

        var result = await client.GetRepositoryDetailAsync("nleceguic", "rush-order");

        result.ReadmeExcerpt.Should().Be("lowercase readme content");
        result.PrimaryLanguage.Should().BeNull();
        result.Topics.Should().BeEmpty();
        result.TopLevelFolders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRepositoryDetailAsync_throws_GitHubIntegrationException_when_the_repository_does_not_exist()
    {
        const string body = """{"data": {"repository": null}, "errors": [{"message": "Could not resolve to a Repository."}]}""";

        _server
            .Given(Request.Create().WithPath("/graphql").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

        var client = CreateClient();

        var act = async () => await client.GetRepositoryDetailAsync("nleceguic", "does-not-exist");

        var exception = await act.Should().ThrowAsync<GitHubIntegrationException>();
        exception.Which.Message.Should().Contain("Could not resolve to a Repository");
    }
}
