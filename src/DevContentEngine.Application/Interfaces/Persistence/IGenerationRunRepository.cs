using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface IGenerationRunRepository : IRepository<GenerationRun>
{
    Task<DateTime?> GetLastRunTimestampAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GenerationRun>> GetLatestAsync(int count, CancellationToken cancellationToken = default);
}
