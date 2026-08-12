namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record GitHubRepositorySnapshot(string Owner, string Name, DateTime CreatedAt);
