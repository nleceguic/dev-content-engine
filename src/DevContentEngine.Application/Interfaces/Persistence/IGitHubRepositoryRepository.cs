using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface IGitHubRepositoryRepository : IRepository<GitHubRepository>
{
    Task<GitHubRepository?> GetByOwnerAndNameAsync(
        string owner,
        string name,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GitHubRepository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default);
}
