using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class ContentIdeaConfiguration : EntityConfigurationBase<ContentIdea>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ContentIdea> builder)
    {
        builder.ToTable("ContentIdeas");

        builder.Property(idea => idea.Origin)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(idea => idea.ActivityScore)
            .IsRequired()
            .HasPrecision(10, 4);

        builder.Property(idea => idea.RelatedActivityIds)
            .HasField("_relatedActivityIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(idea => idea.RelatedTrendId);

        builder.Property(idea => idea.CreatedAt)
            .IsRequired();

        builder.Property(idea => idea.ChosenPath)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne<Trend>()
            .WithMany()
            .HasForeignKey(idea => idea.RelatedTrendId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
