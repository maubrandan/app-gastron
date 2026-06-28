using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.Orders.CloseAndBill;

public sealed record CloseAndBillOrderCommand(
    Guid OrderId,
    byte[] OrderRowVersion,
    byte[] TableRowVersion,
    Guid ClosedByUserId
) : IRequest<Result<Guid>>;

public sealed class CloseAndBillOrderCommandHandler
    : IRequestHandler<CloseAndBillOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IEfConcurrencyHelper _concurrencyHelper;
    private readonly IUnitOfWork _unitOfWork;

    public CloseAndBillOrderCommandHandler(
        IOrderRepository orderRepository,
        ITableRepository tableRepository,
        IEfConcurrencyHelper concurrencyHelper,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _concurrencyHelper = concurrencyHelper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CloseAndBillOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<Guid>.Failure("Pedido no encontrado.");

        var table = await _tableRepository.GetByNumberAsync(order.TableNumber, cancellationToken);
        if (table is null)
            return Result<Guid>.Failure("Mesa no encontrada.");

        _concurrencyHelper.StampRowVersion(order, request.OrderRowVersion);
        _concurrencyHelper.StampRowVersion(table, request.TableRowVersion);

        order.CloseAndBill(request.ClosedByUserId);
        table.Release();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(order.Id);
    }
}
