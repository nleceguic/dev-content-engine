using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class PromptVersionConfiguration : EntityConfigurationBase<PromptVersion>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PromptVersion> builder)
    {
        builder.ToTable("PromptVersions");

        builder.Property(promptVersion => promptVersion.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(promptVersion => promptVersion.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(promptVersion => promptVersion.CreatedAt)
            .IsRequired();

        builder.Property(promptVersion => promptVersion.IsActive)
            .IsRequired();
    }
}
