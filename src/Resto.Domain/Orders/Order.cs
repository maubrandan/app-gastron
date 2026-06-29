using Resto.Domain.Common;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;
using Resto.Domain.Orders.Events;

namespace Resto.Domain.Orders;

public sealed class Order : AggregateRoot
{
    public int TableNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    public Guid CreatedByWaiterId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentToKitchenAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public Money Total { get; private set; } = Money.Zero();
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<OrderLine> _lines = [];
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private Order() { }

    private Order(Guid id, int tableNumber, Guid createdByWaiterId) : base(id)
    {
        TableNumber = tableNumber;
        CreatedByWaiterId = createdByWaiterId;
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Borrador;
        Total = Money.Zero();
    }

    public static Order Create(int tableNumber, Guid createdByWaiterId) =>
        new(Guid.NewGuid(), tableNumber, createdByWaiterId);

    public OrderLine AddLine(Guid productId, Quantity quantity, Money unitPrice, string? notes)
    {
        EnsureModifiable();

        var line = OrderLine.Create(Id, productId, quantity, unitPrice, notes);
        _lines.Add(line);
        RecalculateTotal();
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureModifiable();

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException("La línea de pedido no existe.");

        _lines.Remove(line);
        RecalculateTotal();
    }

    public void ConfirmForKitchen()
    {
        if (Status != OrderStatus.Borrador)
            throw new DomainException("Solo se pueden confirmar pedidos en borrador.");

        if (_lines.Count == 0)
            throw new DomainException("No se puede enviar un pedido vacío a la cocina.");

        Status = OrderStatus.ConfirmadoEnCocina;
        SentToKitchenAt = DateTime.UtcNow;
        AddDomainEvent(new OrderSentToKitchenDomainEvent(Id, TableNumber, SentToKitchenAt.Value));
    }

    public void CloseAndBill(Guid closedByUserId)
    {
        if (Status != OrderStatus.ConfirmadoEnCocina)
            throw new DomainException("Solo se pueden cerrar pedidos confirmados en cocina.");

        Status = OrderStatus.Cerrado;
        ClosedAt = DateTime.UtcNow;
        ClosedByUserId = closedByUserId;
        AddDomainEvent(new OrderClosedDomainEvent(Id, TableNumber));
    }

    public void RequestBill()
    {
        if (Status != OrderStatus.ConfirmadoEnCocina)
            throw new DomainException("Solo se puede solicitar cuenta en pedidos confirmados en cocina.");
    }

    private void EnsureModifiable()
    {
        if (Status != OrderStatus.Borrador)
            throw new DomainException("Solo se pueden modificar pedidos en borrador.");
    }

    private void RecalculateTotal()
    {
        Total = _lines.Aggregate(Money.Zero(), (acc, line) => acc.Add(line.Subtotal));
    }
}
