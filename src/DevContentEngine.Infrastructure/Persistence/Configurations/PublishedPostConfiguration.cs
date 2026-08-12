using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class PublishedPostConfiguration : EntityConfigurationBase<PublishedPost>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PublishedPost> builder)
    {
        builder.ToTable("PublishedPosts");

        builder.Property(post => post.GeneratedPostId)
            .IsRequired();

        builder.Property(post => post.PublishedAt)
            .IsRequired();

        builder.Property(post => post.EngagementNotes)
            .HasColumnType("text");

        builder.HasOne<GeneratedPost>()
            .WithMany()
            .HasForeignKey(post => post.GeneratedPostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(post => post.GeneratedPostId).IsUnique();
    }
}
