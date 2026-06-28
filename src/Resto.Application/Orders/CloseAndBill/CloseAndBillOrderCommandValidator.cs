using FluentValidation;

namespace Resto.Application.Orders.CloseAndBill;

public sealed class CloseAndBillOrderCommandValidator : AbstractValidator<CloseAndBillOrderCommand>
{
    public CloseAndBillOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("El identificador del pedido es obligatorio.");

        RuleFor(x => x.OrderRowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia del pedido es obligatoria.");

        RuleFor(x => x.TableRowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia de la mesa es obligatoria.");
    }
}
