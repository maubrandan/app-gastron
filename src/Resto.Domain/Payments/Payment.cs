using Resto.Domain.Common;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;

namespace Resto.Domain.Payments;

public sealed class Payment : Entity
{
    public Guid OrderId { get; private set; }
    public Guid CashRegisterShiftId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime PaidAt { get; private set; }
    public Guid RegisteredByUserId { get; private set; }

    private Payment() { }

    private Payment(
        Guid id,
        Guid orderId,
        Guid cashRegisterShiftId,
        PaymentMethod method,
        Money amount,
        Guid registeredByUserId) : base(id)
    {
        OrderId = orderId;
        CashRegisterShiftId = cashRegisterShiftId;
        Method = method;
        Amount = amount;
        RegisteredByUserId = registeredByUserId;
        PaidAt = DateTime.UtcNow;
    }

    public static Payment Register(
        Guid orderId,
        Guid cashRegisterShiftId,
        PaymentMethod method,
        Money amount,
        Guid registeredByUserId)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("El pedido es obligatorio para registrar un pago.");

        if (cashRegisterShiftId == Guid.Empty)
            throw new DomainException("El turno de caja es obligatorio para registrar un pago.");

        if (!Enum.IsDefined(method))
            throw new DomainException("El medio de pago no es válido.");

        return new Payment(
            Guid.NewGuid(),
            orderId,
            cashRegisterShiftId,
            method,
            amount,
            registeredByUserId);
    }
}
