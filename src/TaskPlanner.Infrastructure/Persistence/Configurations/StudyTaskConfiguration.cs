using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPlanner.Domain.Entities;

namespace TaskPlanner.Infrastructure.Persistence.Configurations;

public class StudyTaskConfiguration : IEntityTypeConfiguration<StudyTask>
{
    public void Configure(EntityTypeBuilder<StudyTask> builder)
    {
        builder.ToTable("study_tasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.DeadlineUtc);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Sessions).WithOne(x => x.StudyTask).HasForeignKey(x => x.StudyTaskId).OnDelete(DeleteBehavior.Cascade);
    }
}
