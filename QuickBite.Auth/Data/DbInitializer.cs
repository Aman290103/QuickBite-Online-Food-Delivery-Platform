using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuickBite.Auth.Entities;
using System;
using System.Threading.Tasks;

namespace QuickBite.Auth.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AuthDbContext context, UserManager<User> userManager)
        {
            // Seed lordowner@gmail.com
            var ownerId = new Guid("99999999-9999-9999-9999-999999999999");
            var existingOwner = await userManager.FindByIdAsync(ownerId.ToString());
            if (existingOwner == null)
            {
                var owner = new User
                {
                    Id = ownerId,
                    UserName = "lordowner@gmail.com",
                    Email = "lordowner@gmail.com",
                    FullName = "Aman",
                    Role = UserRole.OWNER,
                    Provider = AuthProvider.LOCAL,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(owner, "Password123!");
            }

            // Seed lordadmin@gmail.com
            var adminId = new Guid("88888888-8888-8888-8888-888888888888");
            var existingAdmin = await userManager.FindByIdAsync(adminId.ToString());
            if (existingAdmin == null)
            {
                var admin = new User
                {
                    Id = adminId,
                    UserName = "lordadmin@gmail.com",
                    Email = "lordadmin@gmail.com",
                    FullName = "Aman Admin",
                    Role = UserRole.ADMIN,
                    Provider = AuthProvider.LOCAL,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Password123!");
            }
        }
    }
}
