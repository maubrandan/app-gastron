using FluentAssertions;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;
using Resto.Domain.Orders;
using Resto.Domain.Tables;

namespace Resto.Domain.Tests;

public class OrderTests
{
    private static Order CreateOrderWithLine()
    {
        var order = Order.Create(1, Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), Quantity.Create(2), Money.Create(100m), "Sin cebolla");
        return order;
    }

    [Fact]
    public void ConfirmForKitchen_WithoutLines_ThrowsDomainException()
    {
        var order = Order.Create(1, Guid.NewGuid());

        var act = () => order.ConfirmForKitchen();

        act.Should().Throw<DomainException>()
            .WithMessage("No se puede enviar un pedido vacío a la cocina.");
    }

    [Fact]
    public void ConfirmForKitchen_WithLines_SetsStatusAndTimestamp()
    {
        var order = CreateOrderWithLine();

        order.ConfirmForKitchen();

        order.Status.Should().Be(OrderStatus.ConfirmadoEnCocina);
        order.SentToKitchenAt.Should().NotBeNull();
        order.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "OrderSentToKitchenDomainEvent");
    }

    [Fact]
    public void CloseAndBill_FromConfirmed_SetsCerradoAndClosedAt()
    {
        var closedBy = Guid.NewGuid();
        var order = CreateOrderWithLine();
        order.ConfirmForKitchen();

        order.CloseAndBill(closedBy);

        order.Status.Should().Be(OrderStatus.Cerrado);
        order.ClosedAt.Should().NotBeNull();
        order.ClosedByUserId.Should().Be(closedBy);
    }

    [Fact]
    public void ConfirmForKitchen_WhenNotBorrador_ThrowsDomainException()
    {
        var order = CreateOrderWithLine();
        order.ConfirmForKitchen();

        var act = () => order.ConfirmForKitchen();

        act.Should().Throw<DomainException>()
            .WithMessage("Solo se pueden confirmar pedidos en borrador.");
    }

    [Fact]
    public void AddLine_WhenConfirmadoEnCocina_ThrowsDomainException()
    {
        var order = CreateOrderWithLine();
        order.ConfirmForKitchen();

        var act = () => order.AddLine(Guid.NewGuid(), Quantity.Create(1), Money.Create(10m), null);

        act.Should().Throw<DomainException>()
            .WithMessage("Solo se pueden modificar pedidos en borrador.");
    }

    [Fact]
    public void RemoveLine_WhenConfirmadoEnCocina_ThrowsDomainException()
    {
        var order = CreateOrderWithLine();
        var lineId = order.Lines.First().Id;
        order.ConfirmForKitchen();

        var act = () => order.RemoveLine(lineId);

        act.Should().Throw<DomainException>()
            .WithMessage("Solo se pueden modificar pedidos en borrador.");
    }

    [Fact]
    public void AddLine_WhenClosed_ThrowsDomainException()
    {
        var order = CreateOrderWithLine();
        order.ConfirmForKitchen();
        order.CloseAndBill(Guid.NewGuid());

        var act = () => order.AddLine(Guid.NewGuid(), Quantity.Create(1), Money.Create(10m), null);

        act.Should().Throw<DomainException>()
            .WithMessage("Solo se pueden modificar pedidos en borrador.");
    }

    [Fact]
    public void Total_EqualsSumOfLineSubtotals()
    {
        var order = Order.Create(1, Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), Quantity.Create(2), Money.Create(100m), null);
        order.AddLine(Guid.NewGuid(), Quantity.Create(1), Money.Create(50m), null);

        order.Total.Amount.Should().Be(250m);
    }
}

public class TableTests
{
    [Fact]
    public void Occupy_WhenLibre_SetsAtendiendo()
    {
        var table = Table.Create(5);

        table.Occupy();

        table.Status.Should().Be(TableStatus.Atendiendo);
    }

    [Fact]
    public void Occupy_WhenNotLibre_ThrowsDomainException()
    {
        var table = Table.Create(5);
        table.Occupy();

        var act = () => table.Occupy();

        act.Should().Throw<DomainException>()
            .WithMessage("La mesa no está libre para abrir un pedido.");
    }

    [Fact]
    public void Release_WhenOccupied_SetsLibre()
    {
        var table = Table.Create(3);
        table.Occupy();

        table.Release();

        table.Status.Should().Be(TableStatus.Libre);
    }
}
