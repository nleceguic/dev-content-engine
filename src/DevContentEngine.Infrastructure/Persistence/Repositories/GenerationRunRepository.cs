using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class GenerationRunRepository : RepositoryBase<GenerationRun>, IGenerationRunRepository
{
    public GenerationRunRepository(DevContentEngineDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<DateTime?> GetLastRunTimestampAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.GenerationRuns
            .OrderByDescending(run => run.StartedAt)
            .Select(run => (DateTime?)run.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GenerationRun>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");

        return await DbContext.GenerationRuns
            .OrderByDescending(run => run.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
