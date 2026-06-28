namespace Resto.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);
}
