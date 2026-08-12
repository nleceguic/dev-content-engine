using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Infrastructure.GitHub.GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.GitHub;

public sealed class GitHubClient : IGitHubClient
{
    private const int ReadmeExcerptMaxLength = 4000;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string UserActivityQuery = """
        query UserActivity($username: String!, $since: GitTimestamp!) {
          user(login: $username) {
            repositories(first: 20, orderBy: {field: CREATED_AT, direction: DESC}, ownerAffiliations: [OWNER], isFork: false) {
              nodes {
                name
                owner { login }
                createdAt
                defaultBranchRef {
                  target {
                    ... on Commit {
                      history(since: $since, first: 50) {
                        nodes {
                          oid
                          message
                          committedDate
                          additions
                          deletions
                          changedFilesIfAvailable
                        }
                      }
                    }
                  }
                }
                pullRequests(states: MERGED, first: 20, orderBy: {field: UPDATED_AT, direction: DESC}) {
                  nodes {
                    number
                    title
                    mergedAt
                    files(first: 50) {
                      nodes { path }
                    }
                  }
                }
                issues(states: CLOSED, first: 20, orderBy: {field: UPDATED_AT, direction: DESC}) {
                  nodes {
                    number
                    title
                    closedAt
                    comments(last: 1) {
                      nodes { body }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private const string RepositoryDetailQuery = """
        query RepositoryDetail($owner: String!, $name: String!) {
          repository(owner: $owner, name: $name) {
            description
            primaryLanguage { name }
            repositoryTopics(first: 20) {
              nodes { topic { name } }
            }
            readme: object(expression: "HEAD:README.md") {
              ... on Blob { text }
            }
            readmeLower: object(expression: "HEAD:readme.md") {
              ... on Blob { text }
            }
            rootTree: object(expression: "HEAD:") {
              ... on Tree {
                entries { name type }
              }
            }
          }
        }
        """;

    private readonly HttpClient _httpClient;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubClient> _logger;

    public GitHubClient(HttpClient httpClient, IOptions<GitHubOptions> options, ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GitHubUserActivity> GetUserActivityAsync(
        string username,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var requestPayload = new
        {
            query = UserActivityQuery,
            variables = new
            {
                username,
                since = sinceUtc.ToString("o", CultureInfo.InvariantCulture)
            }
        };

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("graphql", requestPayload, SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            throw new GitHubIntegrationException($"Failed to reach the GitHub GraphQL API: {ex.Message}", ex);
        }

        using (response)
        {
            WarnIfRateLimitIsLow(response);

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, cancellationToken);
                throw new GitHubIntegrationException(
                    $"GitHub GraphQL API returned {(int)response.StatusCode} ({response.StatusCode}): {body}");
            }

            GraphQlResponse<UserActivityData>? payload;

            try
            {
                payload = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserActivityData>>(
                    SerializerOptions,
                    cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new GitHubIntegrationException("GitHub GraphQL API returned a response that could not be parsed.", ex);
            }

            if (payload?.Errors is { Count: > 0 })
            {
                var errorMessage = string.Join("; ", payload.Errors.Select(error => error.Message));
                throw new GitHubIntegrationException($"GitHub GraphQL API returned errors: {errorMessage}");
            }

            var user = payload?.Data?.User
                ?? throw new GitHubIntegrationException($"GitHub GraphQL API returned no data for user '{username}'.");

            return MapToUserActivity(user, sinceUtc);
        }
    }

    public async Task<GitHubRepositoryDetail> GetRepositoryDetailAsync(
        string owner,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var requestPayload = new
        {
            query = RepositoryDetailQuery,
            variables = new { owner, name }
        };

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("graphql", requestPayload, SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            throw new GitHubIntegrationException($"Failed to reach the GitHub GraphQL API: {ex.Message}", ex);
        }

        using (response)
        {
            WarnIfRateLimitIsLow(response);

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, cancellationToken);
                throw new GitHubIntegrationException(
                    $"GitHub GraphQL API returned {(int)response.StatusCode} ({response.StatusCode}): {body}");
            }

            GraphQlResponse<RepositoryDetailData>? payload;

            try
            {
                payload = await response.Content.ReadFromJsonAsync<GraphQlResponse<RepositoryDetailData>>(
                    SerializerOptions,
                    cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new GitHubIntegrationException("GitHub GraphQL API returned a response that could not be parsed.", ex);
            }

            if (payload?.Errors is { Count: > 0 })
            {
                var errorMessage = string.Join("; ", payload.Errors.Select(error => error.Message));
                throw new GitHubIntegrationException($"GitHub GraphQL API returned errors: {errorMessage}");
            }

            var repository = payload?.Data?.Repository
                ?? throw new GitHubIntegrationException($"GitHub GraphQL API returned no data for repository '{owner}/{name}'.");

            return MapToRepositoryDetail(owner, name, repository);
        }
    }

    private void WarnIfRateLimitIsLow(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues) ||
            !response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
        {
            return;
        }

        if (!int.TryParse(limitValues.FirstOrDefault(), out var limit) || limit <= 0 ||
            !int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
        {
            return;
        }

        var remainingRatio = (double)remaining / limit;

        if (remainingRatio <= _options.RateLimitWarningThreshold)
        {
            _logger.LogWarning(
                "GitHub API rate limit is running low: {Remaining}/{Limit} requests remaining ({RemainingRatio:P0}).",
                remaining,
                limit,
                remainingRatio);
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            return "<no response body>";
        }
    }

    private static GitHubUserActivity MapToUserActivity(UserActivityUser user, DateTime sinceUtc)
    {
        var newRepositories = new List<GitHubRepositorySnapshot>();
        var commits = new List<GitHubCommitSnapshot>();
        var pullRequests = new List<GitHubPullRequestSnapshot>();
        var issues = new List<GitHubIssueSnapshot>();

        foreach (var repository in user.Repositories.Nodes)
        {
            var owner = repository.Owner.Login;
            var name = repository.Name;
            var createdAtUtc = repository.CreatedAt.UtcDateTime;

            if (createdAtUtc >= sinceUtc)
            {
                newRepositories.Add(new GitHubRepositorySnapshot(owner, name, createdAtUtc));
            }

            var commitNodes = repository.DefaultBranchRef?.Target?.History?.Nodes ?? [];

            foreach (var commit in commitNodes)
            {
                commits.Add(new GitHubCommitSnapshot(
                    owner,
                    name,
                    commit.Oid,
                    commit.Message,
                    commit.CommittedDate.UtcDateTime,
                    commit.ChangedFilesIfAvailable ?? 0,
                    commit.Additions + commit.Deletions,
                    []));
            }

            foreach (var pullRequest in repository.PullRequests.Nodes)
            {
                if (pullRequest.MergedAt is not { } mergedAt || mergedAt.UtcDateTime < sinceUtc)
                    continue;

                pullRequests.Add(new GitHubPullRequestSnapshot(
                    owner,
                    name,
                    pullRequest.Number.ToString(CultureInfo.InvariantCulture),
                    pullRequest.Title,
                    mergedAt.UtcDateTime,
                    pullRequest.Files.Nodes.Select(file => file.Path).ToList()));
            }

            foreach (var issue in repository.Issues.Nodes)
            {
                if (issue.ClosedAt is not { } closedAt || closedAt.UtcDateTime < sinceUtc)
                    continue;

                var closingContext = issue.Comments.Nodes.Count > 0 ? issue.Comments.Nodes[^1].Body : null;

                issues.Add(new GitHubIssueSnapshot(
                    owner,
                    name,
                    issue.Number.ToString(CultureInfo.InvariantCulture),
                    issue.Title,
                    closingContext,
                    closedAt.UtcDateTime));
            }
        }

        return new GitHubUserActivity(newRepositories, commits, pullRequests, issues);
    }

    private static GitHubRepositoryDetail MapToRepositoryDetail(string owner, string name, RepositoryDetailNode repository)
    {
        var readmeText = repository.Readme?.Text ?? repository.ReadmeLower?.Text;

        var topics = repository.RepositoryTopics.Nodes
            .Select(node => node.Topic.Name)
            .ToList();

        var topLevelFolders = (repository.RootTree?.Entries ?? [])
            .Where(entry => entry.Type == "tree")
            .Select(entry => entry.Name)
            .ToList();

        return new GitHubRepositoryDetail(
            owner,
            name,
            repository.Description,
            TruncateReadme(readmeText),
            topics,
            repository.PrimaryLanguage?.Name,
            topLevelFolders);
    }

    private static string? TruncateReadme(string? readmeText)
    {
        if (string.IsNullOrWhiteSpace(readmeText))
        {
            return null;
        }

        var trimmed = readmeText.Trim();

        return trimmed.Length <= ReadmeExcerptMaxLength
            ? trimmed
            : trimmed[..ReadmeExcerptMaxLength] + "…";
    }
}
