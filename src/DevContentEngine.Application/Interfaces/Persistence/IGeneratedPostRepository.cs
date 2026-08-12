using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface IGeneratedPostRepository : IRepository<GeneratedPost>
{
    Task<IReadOnlyCollection<GeneratedPost>> GetRecentPostsAsync(int days, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GeneratedPost>> GetPendingReviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GeneratedPost>> GetLatestAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, DateTime>> GetLastRepoHighlightByRepositoryAsync(CancellationToken cancellationToken = default);
}
