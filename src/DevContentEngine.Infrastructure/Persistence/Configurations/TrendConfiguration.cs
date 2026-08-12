using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class TrendConfiguration : EntityConfigurationBase<Trend>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Trend> builder)
    {
        builder.ToTable("Trends");

        builder.Property(trend => trend.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(trend => trend.Source)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(trend => trend.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(trend => trend.PublishedAt)
            .IsRequired();

        builder.Property(trend => trend.RelevanceScore)
            .IsRequired()
            .HasPrecision(10, 4);
    }
}
