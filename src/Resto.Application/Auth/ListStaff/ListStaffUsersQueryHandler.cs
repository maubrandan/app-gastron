using MediatR;
using Resto.Application.Auth.Models;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Auth.ListStaff;

public sealed record ListStaffUsersQuery : IRequest<IReadOnlyList<StaffUserDto>>;

public sealed class ListStaffUsersQueryHandler : IRequestHandler<ListStaffUsersQuery, IReadOnlyList<StaffUserDto>>
{
    private readonly IAuthService _authService;

    public ListStaffUsersQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<IReadOnlyList<StaffUserDto>> Handle(
        ListStaffUsersQuery request,
        CancellationToken cancellationToken) =>
        _authService.ListStaffAsync(cancellationToken);
}
