using DevContentEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevContentEngine.Infrastructure.Persistence.Configurations;

public sealed class GenerationRunConfiguration : EntityConfigurationBase<GenerationRun>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GenerationRun> builder)
    {
        builder.ToTable("GenerationRuns");

        builder.Property(run => run.StartedAt)
            .IsRequired();

        builder.Property(run => run.FinishedAt);

        builder.Property(run => run.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(run => run.ChosenPath)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(run => run.TokensUsed);

        builder.Property(run => run.ErrorMessage)
            .HasColumnType("text");

        builder.Property(run => run.ResultingPostId);

        builder.HasOne<GeneratedPost>()
            .WithMany()
            .HasForeignKey(run => run.ResultingPostId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
