namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GitHubUserActivity(
    IReadOnlyCollection<GitHubRepositorySnapshot> NewRepositories,
    IReadOnlyCollection<GitHubCommitSnapshot> Commits,
    IReadOnlyCollection<GitHubPullRequestSnapshot> PullRequests,
    IReadOnlyCollection<GitHubIssueSnapshot> Issues);
