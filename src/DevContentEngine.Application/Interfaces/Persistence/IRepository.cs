using DevContentEngine.Domain.Common;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
