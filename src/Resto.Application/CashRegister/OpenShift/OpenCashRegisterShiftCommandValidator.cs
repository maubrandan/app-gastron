using FluentValidation;

namespace Resto.Application.CashRegister.OpenShift;

public sealed class OpenCashRegisterShiftCommandValidator : AbstractValidator<OpenCashRegisterShiftCommand>
{
    public OpenCashRegisterShiftCommandValidator()
    {
        RuleFor(x => x.OpenedByUserId)
            .NotEmpty()
            .WithMessage("El usuario que abre el turno es obligatorio.");

        RuleFor(x => x.OpeningFloat)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El fondo inicial no puede ser negativo.");
    }
}
