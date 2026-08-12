using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Application.Interfaces.Persistence;

public interface IPromptVersionRepository : IRepository<PromptVersion>
{
    Task<PromptVersion?> GetActiveAsync(PromptRole role, CancellationToken cancellationToken = default);
}
