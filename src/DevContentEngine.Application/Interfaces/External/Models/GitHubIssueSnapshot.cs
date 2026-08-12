namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GitHubIssueSnapshot(
    string RepositoryOwner,
    string RepositoryName,
    string ExternalId,
    string Title,
    string? ClosingContext,
    DateTime ClosedAt);
