using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class GitHubRepositoryRepository : RepositoryBase<GitHubRepository>, IGitHubRepositoryRepository
{
    public GitHubRepositoryRepository(DevContentEngineDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<GitHubRepository?> GetByOwnerAndNameAsync(
        string owner,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.GitHubRepositories
            .FirstOrDefaultAsync(repository => repository.Owner == owner && repository.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyCollection<GitHubRepository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.GitHubRepositories
            .Where(repository => repository.IsActive)
            .ToListAsync(cancellationToken);
    }
}
