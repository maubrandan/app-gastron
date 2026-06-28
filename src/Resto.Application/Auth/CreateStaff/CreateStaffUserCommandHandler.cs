using FluentValidation;
using MediatR;
using Resto.Application.Auth.Models;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.Auth.CreateStaff;

public sealed record CreateStaffUserCommand(
    string Email,
    string Password,
    string DisplayName,
    string Role) : IRequest<Result<Guid>>;

public sealed class CreateStaffUserCommandValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("El email es obligatorio y debe ser válido.");

        RuleFor(x => x.Password)
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("El rol es obligatorio.");
    }
}

public sealed class CreateStaffUserCommandHandler : IRequestHandler<CreateStaffUserCommand, Result<Guid>>
{
    private readonly IAuthService _authService;

    public CreateStaffUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<Guid>> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
    {
        var (success, userId, error) = await _authService.CreateStaffUserAsync(
            new CreateStaffUserRequest(request.Email, request.Password, request.DisplayName, request.Role),
            cancellationToken);

        return success && userId.HasValue
            ? Result<Guid>.Success(userId.Value)
            : Result<Guid>.Failure(error ?? "No se pudo crear el usuario.");
    }
}
