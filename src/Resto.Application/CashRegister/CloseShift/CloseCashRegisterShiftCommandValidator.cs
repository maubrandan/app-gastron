using FluentValidation;

namespace Resto.Application.CashRegister.CloseShift;

public sealed class CloseCashRegisterShiftCommandValidator : AbstractValidator<CloseCashRegisterShiftCommand>
{
    public CloseCashRegisterShiftCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty()
            .WithMessage("El identificador del turno es obligatorio.");

        RuleFor(x => x.ClosedByUserId)
            .NotEmpty()
            .WithMessage("El usuario que cierra el turno es obligatorio.");

        RuleFor(x => x.ClosingCashCounted)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El arqueo de efectivo no puede ser negativo.");
    }
}
