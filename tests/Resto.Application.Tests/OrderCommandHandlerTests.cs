using FluentAssertions;
using NSubstitute;
using Resto.Application.Common.Interfaces;
using Resto.Application.Orders.AddOrderLine;
using Resto.Application.Orders.CloseAndBill;
using Resto.Application.Orders.ConfirmOrderForKitchen;
using Resto.Application.Orders.CreateOrder;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;
using Resto.Domain.Orders;
using Resto.Domain.Payments;
using Resto.Domain.Products;
using Resto.Domain.Tables;

namespace Resto.Application.Tests;

public class OrderCommandHandlerTests
{
    private readonly ITableRepository _tableRepository = Substitute.For<ITableRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICashRegisterShiftRepository _shiftRepository = Substitute.For<ICashRegisterShiftRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IEfConcurrencyHelper _concurrencyHelper = Substitute.For<IEfConcurrencyHelper>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateOrder_WhenTableIsFree_ReturnsOrderAndVersions()
    {
        var waiterId = Guid.NewGuid();
        var table = Table.Create(3);

        _tableRepository.GetByNumberAsync(3, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetActiveByTableNumberAsync(3, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var handler = new CreateOrderCommandHandler(
            _tableRepository,
            _orderRepository,
            _concurrencyHelper,
            _unitOfWork);

        var result = await handler.Handle(
            new CreateOrderCommand(3, waiterId, [1, 2, 3]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().NotBeEmpty();
        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_WhenTableAlreadyHasActiveOrder_ReturnsFailure()
    {
        var existing = Order.Create(3, Guid.NewGuid());
        var table = Table.Create(3);

        _tableRepository.GetByNumberAsync(3, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetActiveByTableNumberAsync(3, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new CreateOrderCommandHandler(
            _tableRepository,
            _orderRepository,
            _concurrencyHelper,
            _unitOfWork);

        var result = await handler.Handle(
            new CreateOrderCommand(3, Guid.NewGuid(), [1]),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("La mesa ya tiene un pedido activo.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderLine_WhenOrderIsBorrador_ReturnsNewRowVersion()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(1, Guid.NewGuid());
        var product = Product.Create("Agua", 1200m, "Bebidas");

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new AddOrderLineCommandHandler(
            _orderRepository,
            _productRepository,
            _concurrencyHelper,
            _unitOfWork);

        var result = await handler.Handle(
            new AddOrderLineCommand(order.Id, productId, 2, "Sin hielo", [1, 2, 3]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().HaveCount(1);
        order.Total.Amount.Should().Be(2400m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderLine_WhenOrderIsConfirmadoEnCocina_ThrowsDomainException()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(1, Guid.NewGuid());
        order.AddLine(productId, Quantity.Create(1), Money.Create(100m), null);
        order.ConfirmForKitchen();

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(Product.Create("Agua", 100m, "Bebidas"));

        var handler = new AddOrderLineCommandHandler(
            _orderRepository,
            _productRepository,
            _concurrencyHelper,
            _unitOfWork);

        var act = () => handler.Handle(
            new AddOrderLineCommand(order.Id, productId, 1, null, [1]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Solo se pueden modificar pedidos en borrador.");
    }

    [Fact]
    public async Task ConfirmForKitchen_WhenOrderHasLines_Succeeds()
    {
        var order = Order.Create(2, Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), Quantity.Create(1), Money.Create(500m), null);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ConfirmOrderForKitchenCommandHandler(
            _orderRepository,
            _concurrencyHelper,
            _unitOfWork);

        var result = await handler.Handle(
            new ConfirmOrderForKitchenCommand(order.Id, [1, 2, 3]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.ConfirmadoEnCocina);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseAndBill_WhenOrderIsConfirmed_ReleasesTable()
    {
        var closedBy = Guid.NewGuid();
        var order = Order.Create(4, Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), Quantity.Create(1), Money.Create(1000m), null);
        order.ConfirmForKitchen();

        var table = Table.Create(4);
        table.Occupy();

        var shift = Resto.Domain.CashRegister.CashRegisterShift.Open(closedBy, Money.Zero());

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _tableRepository.GetByNumberAsync(4, Arg.Any<CancellationToken>()).Returns(table);
        _shiftRepository.GetOpenShiftAsync(Arg.Any<CancellationToken>()).Returns(shift);

        var handler = new CloseAndBillOrderCommandHandler(
            _orderRepository,
            _tableRepository,
            _shiftRepository,
            _paymentRepository,
            _concurrencyHelper,
            _unitOfWork);

        var result = await handler.Handle(
            new CloseAndBillOrderCommand(order.Id, [1], [2], closedBy, PaymentMethod.Cash),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cerrado);
        table.Status.Should().Be(TableStatus.Libre);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
