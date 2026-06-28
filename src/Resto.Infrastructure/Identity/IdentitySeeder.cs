using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Resto.Infrastructure.Identity;

public static class IdentitySeeder
{
    public const string DemoPassword = "Resto123!";

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var role in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
            });

            if (!result.Succeeded)
                logger.LogWarning("No se pudo crear el rol {Role}", role);
        }

        await EnsureUserAsync(userManager, "admin@resto.local", "Administrador", AppRoles.Admin, logger);
        await EnsureUserAsync(userManager, "encargado@resto.local", "Encargado Demo", AppRoles.Manager, logger);
        await EnsureUserAsync(userManager, "mozo1@resto.local", "Mozo Demo", AppRoles.Waiter, logger);
        await EnsureUserAsync(userManager, "mozo2@resto.local", "Mozo 2", AppRoles.Waiter, logger);
        await EnsureUserAsync(userManager, "kitchen@resto.local", "Cocina Kiosk", AppRoles.Kitchen, logger);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string role,
        ILogger logger)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("No se pudo crear el usuario {Email}", email);
            return;
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
