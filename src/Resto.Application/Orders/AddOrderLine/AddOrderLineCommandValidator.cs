using FluentValidation;

namespace Resto.Application.Orders.AddOrderLine;

public sealed class AddOrderLineCommandValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("El identificador del pedido es obligatorio.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("El identificador del producto es obligatorio.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor a cero.");

        RuleFor(x => x.Notes)
            .MaximumLength(250)
            .WithMessage("Las observaciones no pueden superar 250 caracteres.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia es obligatoria.");
    }
}
