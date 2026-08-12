using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevContentEngine.Infrastructure.Persistence;

public sealed class DevContentEngineDbContext : DbContext
{
    public DbSet<GitHubRepository> GitHubRepositories => Set<GitHubRepository>();
    public DbSet<GitHubActivity> GitHubActivities => Set<GitHubActivity>();
    public DbSet<ContentIdea> ContentIdeas => Set<ContentIdea>();
    public DbSet<Trend> Trends => Set<Trend>();
    public DbSet<GeneratedPost> GeneratedPosts => Set<GeneratedPost>();
    public DbSet<PublishedPost> PublishedPosts => Set<PublishedPost>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();
    public DbSet<GenerationRun> GenerationRuns => Set<GenerationRun>();

    public DevContentEngineDbContext(DbContextOptions<DevContentEngineDbContext> options)
        : base(options)
    {
        ChangeTracker.Tracked += (_, args) =>
        {
            if (args.FromQuery && args.Entry.Entity is Entity entity)
            {
                entity.ClearDomainEvents();
            }
        };
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevContentEngineDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
