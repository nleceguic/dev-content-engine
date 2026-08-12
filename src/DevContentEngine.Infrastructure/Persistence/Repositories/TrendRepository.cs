using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class TrendRepository : RepositoryBase<Trend>, ITrendRepository
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public TrendRepository(DevContentEngineDbContext dbContext, IDateTimeProvider dateTimeProvider)
        : base(dbContext)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<Trend>> GetRecentAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days < 0)
            throw new ArgumentOutOfRangeException(nameof(days), days, "Days cannot be negative.");

        var sinceUtc = _dateTimeProvider.UtcNow.AddDays(-days);

        return await DbContext.Trends
            .Where(trend => trend.PublishedAt >= sinceUtc)
            .OrderByDescending(trend => trend.PublishedAt)
            .ToListAsync(cancellationToken);
    }
}
