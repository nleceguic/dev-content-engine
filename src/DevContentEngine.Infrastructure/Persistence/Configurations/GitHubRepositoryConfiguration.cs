using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class GitHubRepositoryConfiguration : EntityConfigurationBase<GitHubRepository>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GitHubRepository> builder)
    {
        builder.ToTable("GitHubRepositories");

        builder.Property(repository => repository.Owner)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(repository => repository.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(repository => repository.DetectedAt)
            .IsRequired();

        builder.Property(repository => repository.IsActive)
            .IsRequired();

        builder.Ignore(repository => repository.FullName);

        builder.HasIndex(repository => new { repository.Owner, repository.Name }).IsUnique();
    }
}
