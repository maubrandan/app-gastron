namespace Resto.Application.Auth.Models;

public sealed record LoginResult(
    string Token,
    DateTime ExpiresAt,
    UserProfile User);

public sealed record UserProfile(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);

public sealed record StaffUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive);

public sealed record CreateStaffUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string Role);
