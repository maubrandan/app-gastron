using FluentAssertions;
using Resto.Domain.CashRegister;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;
using Resto.Domain.Payments;

namespace Resto.Domain.Tests;

public class CashRegisterShiftTests
{
    [Fact]
    public void Open_SetsStatusAndOpeningFloat()
    {
        var userId = Guid.NewGuid();

        var shift = CashRegisterShift.Open(userId, Money.Create(1500m));

        shift.Status.Should().Be(CashShiftStatus.Open);
        shift.OpeningFloat.Amount.Should().Be(1500m);
        shift.OpenedByUserId.Should().Be(userId);
        shift.OpenedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Close_WhenOpen_SetsClosedFields()
    {
        var userId = Guid.NewGuid();
        var shift = CashRegisterShift.Open(userId, Money.Zero());

        shift.Close(userId, Money.Create(3200m));

        shift.Status.Should().Be(CashShiftStatus.Closed);
        shift.ClosedAt.Should().NotBeNull();
        shift.ClosedByUserId.Should().Be(userId);
        shift.ClosingCashCounted!.Amount.Should().Be(3200m);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var shift = CashRegisterShift.Open(userId, Money.Zero());
        shift.Close(userId, Money.Zero());

        var act = () => shift.Close(userId, Money.Zero());

        act.Should().Throw<DomainException>()
            .WithMessage("El turno de caja ya está cerrado.");
    }

    [Fact]
    public void EnsureOpen_WhenClosed_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var shift = CashRegisterShift.Open(userId, Money.Zero());
        shift.Close(userId, Money.Zero());

        var act = () => shift.EnsureOpen();

        act.Should().Throw<DomainException>()
            .WithMessage("No hay turno de caja abierto.");
    }
}

public class PaymentTests
{
    [Fact]
    public void Register_CreatesPaymentWithMethodAndAmount()
    {
        var orderId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var payment = Payment.Register(
            orderId,
            shiftId,
            PaymentMethod.Card,
            Money.Create(4500m),
            userId);

        payment.OrderId.Should().Be(orderId);
        payment.CashRegisterShiftId.Should().Be(shiftId);
        payment.Method.Should().Be(PaymentMethod.Card);
        payment.Amount.Amount.Should().Be(4500m);
        payment.RegisteredByUserId.Should().Be(userId);
        payment.PaidAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
