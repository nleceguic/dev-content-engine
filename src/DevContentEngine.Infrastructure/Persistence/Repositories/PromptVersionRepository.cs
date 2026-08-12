using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class PromptVersionRepository : RepositoryBase<PromptVersion>, IPromptVersionRepository
{
    public PromptVersionRepository(DevContentEngineDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PromptVersion?> GetActiveAsync(PromptRole role, CancellationToken cancellationToken = default)
    {
        return await DbContext.PromptVersions
            .FirstOrDefaultAsync(prompt => prompt.Role == role && prompt.IsActive, cancellationToken);
    }
}
