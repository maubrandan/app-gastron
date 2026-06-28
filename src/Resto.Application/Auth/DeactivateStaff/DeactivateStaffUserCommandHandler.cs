using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.Auth.DeactivateStaff;

public sealed record DeactivateStaffUserCommand(Guid UserId) : IRequest<Result>;

public sealed class DeactivateStaffUserCommandHandler : IRequestHandler<DeactivateStaffUserCommand, Result>
{
    private readonly IAuthService _authService;

    public DeactivateStaffUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result> Handle(DeactivateStaffUserCommand request, CancellationToken cancellationToken)
    {
        var (success, error) = await _authService.DeactivateStaffUserAsync(request.UserId, cancellationToken);
        return success ? Result.Success() : Result.Failure(error ?? "No se pudo desactivar el usuario.");
    }
}
