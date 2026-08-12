using DevContentEngine.Application.Interfaces.Persistence;

namespace DevContentEngine.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DevContentEngineDbContext _dbContext;

    public UnitOfWork(DevContentEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
