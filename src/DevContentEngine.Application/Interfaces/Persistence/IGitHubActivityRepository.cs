using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface IGitHubActivityRepository : IRepository<GitHubActivity>
{
    Task AddRangeAsync(IEnumerable<GitHubActivity> activities, CancellationToken cancellationToken = default);

    Task<GitHubActivity?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GitHubActivity>> GetRecentByRepositoryAsync(
        Guid repositoryId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
}
