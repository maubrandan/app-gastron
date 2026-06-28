using Resto.Domain.Common;
using Resto.Domain.Common.ValueObjects;

namespace Resto.Domain.Orders;

public sealed class OrderLine : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Quantity Quantity { get; private set; } = Quantity.Create(1);
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money Subtotal { get; private set; } = Money.Zero();
    public string? Notes { get; private set; }

    private OrderLine() { }

    internal OrderLine(
        Guid id,
        Guid orderId,
        Guid productId,
        Quantity quantity,
        Money unitPrice,
        string? notes) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Notes = notes;
        Subtotal = Money.Create(unitPrice.Amount * quantity.Value, unitPrice.Currency);
    }

    internal static OrderLine Create(
        Guid orderId,
        Guid productId,
        Quantity quantity,
        Money unitPrice,
        string? notes) =>
        new(Guid.NewGuid(), orderId, productId, quantity, unitPrice, notes);
}
