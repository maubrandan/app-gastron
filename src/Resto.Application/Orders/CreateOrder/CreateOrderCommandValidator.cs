using FluentValidation;

namespace Resto.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.TableNumber)
            .GreaterThan(0)
            .WithMessage("El número de mesa es obligatorio.");

        RuleFor(x => x.WaiterId)
            .NotEmpty()
            .WithMessage("El identificador del mozo es obligatorio.");

        RuleFor(x => x.TableRowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia de la mesa es obligatoria.");
    }
}
