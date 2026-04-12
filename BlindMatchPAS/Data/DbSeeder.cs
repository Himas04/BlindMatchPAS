using BlindMatchPAS.Models;
using Microsoft.AspNetCore.Identity;

namespace BlindMatchPAS.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Student", "Supervisor", "ModuleLeader", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed default admin
            var adminEmail = "admin@blindmatch.ac.lk";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    FullName = "System Administrator",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Role = "Admin"
                };
                await userManager.CreateAsync(admin, "Admin@1234");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed default module leader
            var leaderEmail = "moduleleader@blindmatch.ac.lk";
            if (await userManager.FindByEmailAsync(leaderEmail) == null)
            {
                var leader = new ApplicationUser
                {
                    FullName = "Module Leader",
                    UserName = leaderEmail,
                    Email = leaderEmail,
                    EmailConfirmed = true,
                    Role = "ModuleLeader"
                };
                await userManager.CreateAsync(leader, "Leader@1234");
                await userManager.AddToRoleAsync(leader, "ModuleLeader");
            }
        }
    }
}
