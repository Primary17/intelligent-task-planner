using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPlanner.Domain.Entities;

namespace TaskPlanner.Infrastructure.Persistence.Configurations;

public class DisciplineConfiguration : IEntityTypeConfiguration<Discipline>
{
    public void Configure(EntityTypeBuilder<Discipline> builder)
    {
        builder.ToTable("disciplines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Color).IsRequired().HasMaxLength(16);
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        builder.HasMany(x => x.Tasks).WithOne(x => x.Discipline).HasForeignKey(x => x.DisciplineId).OnDelete(DeleteBehavior.Cascade);
    }
}
