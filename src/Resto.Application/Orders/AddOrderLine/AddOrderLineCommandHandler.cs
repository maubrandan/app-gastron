using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Common.ValueObjects;

namespace Resto.Application.Orders.AddOrderLine;

public sealed record AddOrderLineCommand(
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    string? Notes,
    byte[] RowVersion
) : IRequest<Result<string>>;

public sealed class AddOrderLineCommandHandler
    : IRequestHandler<AddOrderLineCommand, Result<string>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IEfConcurrencyHelper _concurrencyHelper;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderLineCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IEfConcurrencyHelper concurrencyHelper,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _concurrencyHelper = concurrencyHelper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(
        AddOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<string>.Failure("Pedido no encontrado.");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<string>.Failure("Producto no encontrado.");

        if (!product.IsActive)
            return Result<string>.Failure("El producto no está disponible en la carta.");

        var line = order.AddLine(
            request.ProductId,
            Quantity.Create(request.Quantity),
            product.Price,
            request.Notes);
        _orderRepository.RegisterNewLine(line);
        _concurrencyHelper.StampRowVersion(order, request.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(Common.Helpers.RowVersionHelper.ToBase64(order.RowVersion));
    }
}
