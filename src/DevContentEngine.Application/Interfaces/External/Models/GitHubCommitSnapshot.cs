namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GitHubCommitSnapshot(
    string RepositoryOwner,
    string RepositoryName,
    string Sha,
    string Message,
    DateTime CommittedAt,
    int FilesChanged,
    int LinesChanged,
    IReadOnlyCollection<string> ChangedFilePaths);
