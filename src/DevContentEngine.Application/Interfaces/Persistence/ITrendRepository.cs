using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface ITrendRepository : IRepository<Trend>
{
    Task<IReadOnlyCollection<Trend>> GetRecentAsync(int days, CancellationToken cancellationToken = default);
}
