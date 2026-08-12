namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GitHubRepositoryDetail(
    string Owner,
    string Name,
    string? Description,
    string? ReadmeExcerpt,
    IReadOnlyCollection<string> Topics,
    string? PrimaryLanguage,
    IReadOnlyCollection<string> TopLevelFolders);
