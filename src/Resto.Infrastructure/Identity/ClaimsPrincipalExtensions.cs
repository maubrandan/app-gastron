using System.Security.Claims;

namespace Resto.Infrastructure.Identity;

internal static class ClaimsPrincipalExtensions
{
    private static readonly string[] RoleClaimTypes =
    [
        "role",
        ClaimTypes.Role,
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    ];

    internal static bool HasAnyRole(this ClaimsPrincipal? user, params string[] roles)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var userRoles = user.Claims
            .Where(c => RoleClaimTypes.Contains(c.Type))
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return roles.Any(userRoles.Contains);
    }
}
