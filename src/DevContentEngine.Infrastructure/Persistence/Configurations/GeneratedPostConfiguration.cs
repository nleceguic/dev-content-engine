using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class GeneratedPostConfiguration : EntityConfigurationBase<GeneratedPost>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GeneratedPost> builder)
    {
        builder.ToTable("GeneratedPosts");

        builder.Property(post => post.ContentIdeaId)
            .IsRequired();

        builder.Property(post => post.Hook)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(post => post.Body)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(post => post.Conclusion)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(post => post.Cta)
            .HasColumnType("text");

        builder.Property(post => post.Hashtags)
            .HasField("_hashtags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(post => post.Sources)
            .HasField("_sources")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(post => post.ImagePrompt)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(post => post.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(post => post.Origin)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(post => post.PromptVersionId)
            .IsRequired();

        builder.Property(post => post.CreatedAt)
            .IsRequired();

        builder.Property(post => post.UpdatedAt)
            .IsRequired();

        builder.HasOne<ContentIdea>()
            .WithMany()
            .HasForeignKey(post => post.ContentIdeaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PromptVersion>()
            .WithMany()
            .HasForeignKey(post => post.PromptVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(post => post.CreatedAt);

        builder.HasIndex(post => post.Hashtags).HasMethod("gin");
    }
}
