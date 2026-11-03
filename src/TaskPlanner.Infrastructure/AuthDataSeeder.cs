using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskPlanner.Application.Security;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;
using TaskPlanner.Infrastructure.Security;

namespace TaskPlanner.Infrastructure;

public static class AuthDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@taskplanner.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";

        if (await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            DisplayName = "System Administrator",
            Role = UserRole.Admin,
            CreatedAtUtc = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.Hash(admin, adminPassword);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
