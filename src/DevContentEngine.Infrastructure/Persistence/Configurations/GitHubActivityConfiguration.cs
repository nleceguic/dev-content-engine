using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class GitHubActivityConfiguration : EntityConfigurationBase<GitHubActivity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GitHubActivity> builder)
    {
        builder.ToTable("GitHubActivities");

        builder.Property(activity => activity.RepositoryId)
            .IsRequired();

        builder.Property(activity => activity.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(activity => activity.DetectedTechnologies)
            .HasField("_detectedTechnologies")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(activity => activity.Summary)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(activity => activity.ExternalId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(activity => activity.Timestamp)
            .IsRequired();

        builder.Property(activity => activity.IsNoise)
            .IsRequired();

        builder.HasOne<GitHubRepository>()
            .WithMany()
            .HasForeignKey(activity => activity.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(activity => activity.ExternalId).IsUnique();

        builder.HasIndex(activity => new { activity.RepositoryId, activity.Timestamp });
    }
}
