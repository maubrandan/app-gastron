using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Orders;
using Resto.Domain.Tables;

namespace Resto.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(
    int TableNumber,
    Guid WaiterId,
    byte[] TableRowVersion
) : IRequest<Result<CreateOrderResponse>>;

public sealed record CreateOrderResponse(Guid OrderId, string OrderRowVersion, string TableRowVersion);

public sealed class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    private readonly ITableRepository _tableRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IEfConcurrencyHelper _concurrencyHelper;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        ITableRepository tableRepository,
        IOrderRepository orderRepository,
        IEfConcurrencyHelper concurrencyHelper,
        IUnitOfWork unitOfWork)
    {
        _tableRepository = tableRepository;
        _orderRepository = orderRepository;
        _concurrencyHelper = concurrencyHelper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var table = await _tableRepository.GetByNumberAsync(request.TableNumber, cancellationToken);
        if (table is null)
            return Result<CreateOrderResponse>.Failure("Mesa no encontrada.");

        var existing = await _orderRepository.GetActiveByTableNumberAsync(request.TableNumber, cancellationToken);
        if (existing is not null)
            return Result<CreateOrderResponse>.Failure("La mesa ya tiene un pedido activo.");

        _concurrencyHelper.StampRowVersion(table, request.TableRowVersion);
        table.Occupy();

        var order = Order.Create(request.TableNumber, request.WaiterId);
        await _orderRepository.AddAsync(order, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateOrderResponse>.Success(new CreateOrderResponse(
            order.Id,
            Common.Helpers.RowVersionHelper.ToBase64(order.RowVersion),
            Common.Helpers.RowVersionHelper.ToBase64(table.RowVersion)));
    }
}
