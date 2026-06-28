using MediatR;
using Resto.Application.Auth.Models;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult?>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        _authService.LoginAsync(request.Email, request.Password, cancellationToken);
}
