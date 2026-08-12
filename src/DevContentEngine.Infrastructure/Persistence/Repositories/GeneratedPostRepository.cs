using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class GeneratedPostRepository : RepositoryBase<GeneratedPost>, IGeneratedPostRepository
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public GeneratedPostRepository(DevContentEngineDbContext dbContext, IDateTimeProvider dateTimeProvider)
        : base(dbContext)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<GeneratedPost>> GetRecentPostsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days < 0)
            throw new ArgumentOutOfRangeException(nameof(days), days, "Days cannot be negative.");

        var sinceUtc = _dateTimeProvider.UtcNow.AddDays(-days);

        return await DbContext.GeneratedPosts
            .Where(post => post.CreatedAt >= sinceUtc)
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GeneratedPost>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.GeneratedPosts
            .Where(post => post.Status == GeneratedPostStatus.Draft || post.Status == GeneratedPostStatus.Edited)
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GeneratedPost>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");

        return await DbContext.GeneratedPosts
            .OrderByDescending(post => post.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
