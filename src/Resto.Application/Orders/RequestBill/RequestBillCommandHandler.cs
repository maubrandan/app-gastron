using FluentValidation;
using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Exceptions;

namespace Resto.Application.Orders.RequestBill;

public sealed record RequestBillCommand(
    Guid OrderId,
    byte[] OrderRowVersion,
    byte[] TableRowVersion
) : IRequest<Result<Guid>>;

public sealed class RequestBillCommandValidator : AbstractValidator<RequestBillCommand>
{
    public RequestBillCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("El identificador del pedido es obligatorio.");

        RuleFor(x => x.OrderRowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia del pedido es obligatoria.");

        RuleFor(x => x.TableRowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia de la mesa es obligatoria.");
    }
}

public sealed class RequestBillCommandHandler
    : IRequestHandler<RequestBillCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IEfConcurrencyHelper _concurrencyHelper;
    private readonly IUnitOfWork _unitOfWork;

    public RequestBillCommandHandler(
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
        RequestBillCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<Guid>.Failure("Pedido no encontrado.");

        var table = await _tableRepository.GetByNumberAsync(order.TableNumber, cancellationToken);
        if (table is null)
            return Result<Guid>.Failure("Mesa no encontrada.");

        try
        {
            _concurrencyHelper.StampRowVersion(order, request.OrderRowVersion);
            _concurrencyHelper.StampRowVersion(table, request.TableRowVersion);

            order.RequestBill();
            table.MarkWaitingForBill();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(order.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
