namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GitHubPullRequestSnapshot(
    string RepositoryOwner,
    string RepositoryName,
    string ExternalId,
    string Title,
    DateTime MergedAt,
    IReadOnlyCollection<string> ChangedFilePaths);
