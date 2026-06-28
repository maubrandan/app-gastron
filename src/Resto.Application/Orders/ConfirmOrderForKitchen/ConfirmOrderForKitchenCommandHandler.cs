using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.Orders.ConfirmOrderForKitchen;

public sealed record ConfirmOrderForKitchenCommand(
    Guid OrderId,
    byte[] RowVersion
) : IRequest<Result<Guid>>;

public sealed class ConfirmOrderForKitchenCommandHandler
    : IRequestHandler<ConfirmOrderForKitchenCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEfConcurrencyHelper _concurrencyHelper;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderForKitchenCommandHandler(
        IOrderRepository orderRepository,
        IEfConcurrencyHelper concurrencyHelper,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _concurrencyHelper = concurrencyHelper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        ConfirmOrderForKitchenCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Result<Guid>.Failure("Pedido no encontrado.");

        _concurrencyHelper.StampRowVersion(order, request.RowVersion);
        order.ConfirmForKitchen();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(order.Id);
    }
}
