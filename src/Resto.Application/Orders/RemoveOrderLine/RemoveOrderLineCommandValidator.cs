using FluentValidation;

namespace Resto.Application.Orders.RemoveOrderLine;

public sealed class RemoveOrderLineCommandValidator : AbstractValidator<RemoveOrderLineCommand>
{
    public RemoveOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("El identificador del pedido es obligatorio.");

        RuleFor(x => x.LineId)
            .NotEmpty()
            .WithMessage("El identificador de la línea es obligatorio.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia es obligatoria.");
    }
}
