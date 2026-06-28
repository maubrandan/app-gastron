using MediatR;
using Resto.Application.Auth.Models;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<UserProfile?>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfile?>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthService _authService;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IAuthService authService)
    {
        _currentUser = currentUser;
        _authService = authService;
    }

    public async Task<UserProfile?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return null;

        return await _authService.GetProfileAsync(userId, cancellationToken);
    }
}
