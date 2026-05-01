using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data;

public static class DbSeeder
{
    public static async Task SeedCommissionSettingsAsync(AppDbContext db)
    {
        const string key = "GlobalCommissionRate";

        var exists = await db.Settings.AnyAsync(s => s.Key == key);
        if (!exists)
        {
            db.Settings.Add(new Setting
            {
                Key = key,
                Value = "0.10",
                UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }

    public static async Task SeedSuperAdminAsync(AppDbContext db)
    {
        Console.WriteLine("🔎 Vérification SuperAdmin...");

        var exists = await db.Users.AnyAsync(u =>
            u.Username == "superadmin" || u.Role == "SuperAdmin");

        if (exists)
        {
            Console.WriteLine("ℹ️ SuperAdmin existe déjà.");
            return;
        }

        db.Users.Add(new User
        {
            Username = "superadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "SuperAdmin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        Console.WriteLine("✅ SuperAdmin créé automatiquement : superadmin / admin123");
    }
}