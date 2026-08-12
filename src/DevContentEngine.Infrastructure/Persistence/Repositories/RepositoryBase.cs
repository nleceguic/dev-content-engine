using DevContentEngine.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public abstract class RepositoryBase<TEntity> where TEntity : Entity
{
    protected readonly DevContentEngineDbContext DbContext;

    protected RepositoryBase(DevContentEngineDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<TEntity>().Add(entity);
        return Task.CompletedTask;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }
}
