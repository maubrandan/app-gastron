using Resto.Domain.Common;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;

namespace Resto.Domain.CashRegister;

public sealed class CashRegisterShift : AggregateRoot
{
    public DateTime OpenedAt { get; private set; }
    public Guid OpenedByUserId { get; private set; }
    public Money OpeningFloat { get; private set; } = Money.Zero();
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public Money? ClosingCashCounted { get; private set; }
    public CashShiftStatus Status { get; private set; }

    private CashRegisterShift() { }

    private CashRegisterShift(Guid id, Guid openedByUserId, Money openingFloat) : base(id)
    {
        OpenedByUserId = openedByUserId;
        OpeningFloat = openingFloat;
        OpenedAt = DateTime.UtcNow;
        Status = CashShiftStatus.Open;
    }

    public static CashRegisterShift Open(Guid openedByUserId, Money openingFloat)
    {
        if (openedByUserId == Guid.Empty)
            throw new DomainException("El usuario que abre el turno es obligatorio.");

        return new CashRegisterShift(Guid.NewGuid(), openedByUserId, openingFloat);
    }

    public void Close(Guid closedByUserId, Money closingCashCounted)
    {
        if (Status != CashShiftStatus.Open)
            throw new DomainException("El turno de caja ya está cerrado.");

        if (closedByUserId == Guid.Empty)
            throw new DomainException("El usuario que cierra el turno es obligatorio.");

        Status = CashShiftStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedByUserId = closedByUserId;
        ClosingCashCounted = closingCashCounted;
    }

    public void EnsureOpen()
    {
        if (Status != CashShiftStatus.Open)
            throw new DomainException("No hay turno de caja abierto.");
    }
}
