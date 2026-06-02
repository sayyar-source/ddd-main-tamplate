using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Enums;

namespace PrintBridge.Infrastructure.Data.Extensions;

public static class SeedDataExtension
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintBridgeDbContext>();

            try
            {
                // Seed initial data if database is empty
                await SeedDatabaseAsync(dbContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during database initialization");
                throw;
            }
        }
    }

    private static async Task SeedDatabaseAsync(PrintBridgeDbContext dbContext)
    {
        try
        {
            Log.Information("Checking existing data for seeding...");

            var hasher = new PasswordHasher<object>();

            // -------------------------
            // Seed Accounts (Admin, User, Supplier)
            // -------------------------
            if (!await dbContext.Accounts.AnyAsync())
            {
                Log.Information("Seeding default accounts (Admin, User, Supplier)...");

                var admin = new Account
                {
                    Username = "admin",
                    Email = "admin@PrintBridge.local",
                    PasswordHash = hasher.HashPassword(new object(), "Admin@123"),
                    FullName = "Portal Administrator",
                    IsActive = true,
                    Role = AccountRole.Admin,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                var user = new Account
                {
                    Username = "user1",
                    Email = "user1@PrintBridge.local",
                    PasswordHash = hasher.HashPassword(new object(), "User@123"),
                    FullName = "Demo User",
                    IsActive = true,
                    Role = AccountRole.User,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                var supplierAccount1 = new Account
                {
                    Username = "supplier1",
                    Email = "contact@abctrading.com",
                    PasswordHash = hasher.HashPassword(new object(), "abc@123"),
                    FullName = "ABC Trading Company",
                    IsActive = true,
                    Role = AccountRole.Supplier,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                var supplierAccount2 = new Account
                {
                    Username = "supplier2",
                    Email = "contact2@xyzmanufacturing.com",
                    PasswordHash = hasher.HashPassword(new object(), "abc@123"),
                    FullName = "XYZ Manufacturing Ltd",
                    IsActive = true,
                    Role = AccountRole.Supplier,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await dbContext.Accounts.AddRangeAsync(admin, user, supplierAccount1, supplierAccount2);
                await dbContext.SaveChangesAsync();

                Log.Information("Added {Count} accounts", 4);

         


                await dbContext.SaveChangesAsync();
                Log.Information("Added supplier profiles for seeded supplier accounts.");
            }
            else
            {
                Log.Information("Accounts exist; skipping account seeding.");
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while seeding database");
            throw;
        }
    }
}