using Microsoft.AspNetCore.Identity;
using Resto.Application.Auth.Models;
using Resto.Application.Common.Interfaces;

namespace Resto.Infrastructure.Identity;

public sealed class IdentityAuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public IdentityAuthService(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return null;

        if (!await _userManager.CheckPasswordAsync(user, password))
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, roles);

        return new LoginResult(token, expiresAt, MapProfile(user, roles));
    }

    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapProfile(user, roles);
    }

    public async Task<IReadOnlyList<StaffUserDto>> ListStaffAsync(CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.OrderBy(u => u.DisplayName).ToList();
        var result = new List<StaffUserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new StaffUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                roles.ToList(),
                user.IsActive));
        }

        return result;
    }

    public async Task<(bool Success, Guid? UserId, string? Error)> CreateStaffUserAsync(
        CreateStaffUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(request.Role))
            return (false, null, "Rol inválido.");

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return (false, null, "Ya existe un usuario con ese email.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            IsActive = true,
            EmailConfirmed = true,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var error = createResult.Errors.FirstOrDefault()?.Description ?? "No se pudo crear el usuario.";
            return (false, null, error);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            var error = roleResult.Errors.FirstOrDefault()?.Description ?? "No se pudo asignar el rol.";
            return (false, null, error);
        }

        return (true, user.Id, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateStaffUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, "Usuario no encontrado.");

        if (!user.IsActive)
            return (false, "El usuario ya está desactivado.");

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "No se pudo desactivar el usuario.";
            return (false, error);
        }

        return (true, null);
    }

    private static UserProfile MapProfile(ApplicationUser user, IList<string> roles) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList());
}
