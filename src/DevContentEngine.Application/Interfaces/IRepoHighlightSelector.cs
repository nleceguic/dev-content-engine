using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Interfaces;

public interface IRepoHighlightSelector
{
    Task<GitHubRepository?> SelectAsync(CancellationToken cancellationToken = default);
}
