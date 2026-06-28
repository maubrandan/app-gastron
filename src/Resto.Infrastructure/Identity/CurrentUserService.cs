using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Resto.Application.Common.Interfaces;

namespace Resto.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public IReadOnlyList<string> Roles =>
        _httpContextAccessor.HttpContext?.User.Claims
            .Where(c => c.Type is "role" or ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
        ?? [];

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.HasAnyRole(role) ?? false;
}
