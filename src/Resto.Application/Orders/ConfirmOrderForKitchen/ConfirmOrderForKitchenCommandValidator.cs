using FluentValidation;

namespace Resto.Application.Orders.ConfirmOrderForKitchen;

public sealed class ConfirmOrderForKitchenCommandValidator
    : AbstractValidator<ConfirmOrderForKitchenCommand>
{
    public ConfirmOrderForKitchenCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("El identificador del pedido es obligatorio.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia es obligatoria.");
    }
}
