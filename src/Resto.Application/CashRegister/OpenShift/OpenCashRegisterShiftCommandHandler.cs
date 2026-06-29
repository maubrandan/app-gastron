using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.CashRegister;

namespace Resto.Application.CashRegister.OpenShift;

public sealed record OpenCashRegisterShiftCommand(
    Guid OpenedByUserId,
    decimal OpeningFloat
) : IRequest<Result<Guid>>;

public sealed class OpenCashRegisterShiftCommandHandler
    : IRequestHandler<OpenCashRegisterShiftCommand, Result<Guid>>
{
    private readonly ICashRegisterShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OpenCashRegisterShiftCommandHandler(
        ICashRegisterShiftRepository shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        OpenCashRegisterShiftCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _shiftRepository.GetOpenShiftAsync(cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("Ya hay un turno de caja abierto.");

        var shift = CashRegisterShift.Open(
            request.OpenedByUserId,
            Money.Create(request.OpeningFloat));

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(shift.Id);
    }
}
