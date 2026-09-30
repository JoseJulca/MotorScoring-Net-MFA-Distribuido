using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotorScoring.Identity.Models;
using MotorScoring.Identity.Security;

namespace MotorScoring.Identity.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        const string adminRole = "Administrador";
        const string analystRole = "Analista";

        foreach (var roleName in new[] { adminRole, analystRole })
            if (await roleManager.FindByNameAsync(roleName) is null)
            {
                var created = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
            }

        foreach (var roleName in new[] { adminRole, analystRole })
        {
            var role = await roleManager.FindByNameAsync(roleName) ?? throw new InvalidOperationException($"Rol {roleName} no encontrado.");
            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var permission in new[] { SecurityConstants.Permissions.ScoringRegistrar, SecurityConstants.Permissions.ScoringEvaluar })
                if (!existingClaims.Any(x => x.Type == SecurityConstants.PermissionClaim && x.Value == permission))
                    await roleManager.AddClaimAsync(role, new Claim(SecurityConstants.PermissionClaim, permission));
        }

        var email = configuration["Seed:AdminEmail"] ?? "admin@motorscoring.local";
        var password = configuration["Seed:AdminPassword"] ?? "Admin1234*";
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Administrador Motor Scoring", LockoutEnabled = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!await userManager.IsInRoleAsync(user, adminRole)) await userManager.AddToRoleAsync(user, adminRole);
    }
}
