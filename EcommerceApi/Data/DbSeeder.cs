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
                Value = "0.10", // 10% par défaut
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }
}