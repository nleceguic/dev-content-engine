using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;

namespace DevContentEngine.Infrastructure.Persistence.Repositories;

public sealed class ContentIdeaRepository : RepositoryBase<ContentIdea>, IContentIdeaRepository
{
    public ContentIdeaRepository(DevContentEngineDbContext dbContext)
        : base(dbContext)
    {
    }
}
