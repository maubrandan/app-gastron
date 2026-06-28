# Ejemplos — generate-cqrs-command

## ConfirmOrderForKitchen

Comando que confirma un pedido en borrador y lo envía a cocina.

**Ubicación:** `Resto.Application/Orders/ConfirmOrderForKitchen/`

### ConfirmOrderForKitchenCommand.cs

```csharp
using MediatR;
using Resto.Application.Common.Models;

namespace Resto.Application.Orders.ConfirmOrderForKitchen;

public sealed record ConfirmOrderForKitchenCommand(
    Guid OrderId,
    byte[] RowVersion
) : IRequest<Result<Guid>>;
```

### ConfirmOrderForKitchenCommandValidator.cs

```csharp
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
```

### ConfirmOrderForKitchenCommandHandler.cs

```csharp
using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.Orders.ConfirmOrderForKitchen;

public sealed class ConfirmOrderForKitchenCommandHandler
    : IRequestHandler<ConfirmOrderForKitchenCommand, Result<Guid>>
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderForKitchenCommandHandler(
        IPedidoRepository pedidoRepository,
        IUnitOfWork unitOfWork)
    {
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        ConfirmOrderForKitchenCommand request,
        CancellationToken cancellationToken)
    {
        var pedido = await _pedidoRepository
            .GetByIdAsync(request.OrderId, cancellationToken);

        if (pedido is null)
            return Result<Guid>.Failure("Pedido no encontrado.");

        // Borrador → ConfirmadoEnCocina; lanza DomainException si la transición es inválida
        pedido.ConfirmForKitchen();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // OrderConfirmedForKitchenEvent → dispatcher → SignalR (cocina y salón)

        return Result<Guid>.Success(pedido.Id);
    }
}
```

### Agregado (referencia en Domain)

```csharp
// Resto.Domain/Orders/Pedido.cs
public void ConfirmForKitchen()
{
    if (Status != OrderStatus.Borrador)
        throw new DomainException("Solo se pueden confirmar pedidos en borrador.");

    Status = OrderStatus.ConfirmadoEnCocina;
    AddDomainEvent(new OrderConfirmedForKitchenEvent(Id, TableId));
}
```

## MarkOrderAsReady

Comando que marca un pedido como listo (ConfirmadoEnCocina → Listo).

**Propiedades del comando:**

```csharp
public sealed record MarkOrderAsReadyCommand(
    Guid OrderId,
    byte[] RowVersion
) : IRequest<Result<Guid>>;
```

**Método semántico:** `pedido.MarkAsReady()`

**Evento esperado:** `OrderMarkedAsReadyEvent`
