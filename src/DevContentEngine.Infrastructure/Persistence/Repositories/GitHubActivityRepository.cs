using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class GitHubActivityRepository : RepositoryBase<GitHubActivity>, IGitHubActivityRepository
{
    public GitHubActivityRepository(DevContentEngineDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task AddRangeAsync(IEnumerable<GitHubActivity> activities, CancellationToken cancellationToken = default)
    {
        await DbContext.GitHubActivities.AddRangeAsync(activities, cancellationToken);
    }

    public async Task<GitHubActivity?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await DbContext.GitHubActivities
            .FirstOrDefaultAsync(activity => activity.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<GitHubActivity>> GetRecentByRepositoryAsync(
        Guid repositoryId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.GitHubActivities
            .Where(activity => activity.RepositoryId == repositoryId && activity.Timestamp >= sinceUtc)
            .OrderBy(activity => activity.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
