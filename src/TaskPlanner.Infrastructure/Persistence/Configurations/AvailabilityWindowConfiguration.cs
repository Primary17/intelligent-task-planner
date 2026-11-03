using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPlanner.Domain.Entities;

namespace TaskPlanner.Infrastructure.Persistence.Configurations;

public class AvailabilityWindowConfiguration : IEntityTypeConfiguration<AvailabilityWindow>
{
    public void Configure(EntityTypeBuilder<AvailabilityWindow> builder)
    {
        builder.ToTable("availability_windows");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.DayOfWeek });
    }
}
