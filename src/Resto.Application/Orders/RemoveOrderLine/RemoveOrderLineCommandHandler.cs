using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.Orders.RemoveOrderLine;

public sealed record RemoveOrderLineCommand(
    Guid OrderId,
    Guid LineId,
    byte[] RowVersion
) : IRequest<Result<string>>;

public sealed class RemoveOrderLineCommandHandler
    : IRequestHandler<RemoveOrderLineCommand, Result<string>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEfConcurrencyHelper _concurrencyHelper;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveOrderLineCommandHandler(
        IOrderRepository orderRepository,
        IEfConcurrencyHelper concurrencyHelper,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _concurrencyHelper = concurrencyHelper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(
        RemoveOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<string>.Failure("Pedido no encontrado.");

        order.RemoveLine(request.LineId);
        _concurrencyHelper.StampRowVersion(order, request.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(Common.Helpers.RowVersionHelper.ToBase64(order.RowVersion));
    }
}
