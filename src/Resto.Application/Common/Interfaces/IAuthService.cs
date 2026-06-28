using Resto.Application.Auth.Models;

namespace Resto.Application.Common.Interfaces;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffUserDto>> ListStaffAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, Guid? UserId, string? Error)> CreateStaffUserAsync(
        CreateStaffUserRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeactivateStaffUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
