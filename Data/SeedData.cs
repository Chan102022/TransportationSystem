using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;

namespace TransportationBookingSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = new[] { "Admin", "Conductor", "User" };

            // Create roles if not existing
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create default conductor account
            string conductorEmail = "conductor@tbs.com";
            string conductorPassword = "Conductor@123";

            var existingConductor = await userManager.FindByEmailAsync(conductorEmail);

            if (existingConductor == null)
            {
                var conductor = new ApplicationUser
                {
                    UserName = conductorEmail,
                    Email = conductorEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(conductor, conductorPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(conductor, "Conductor");
                }
            }

            // OPTIONAL: Default Admin (you can skip if you have one)
            string adminEmail = "admin@tbs.com";
            string adminPassword = "Admin@123";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
