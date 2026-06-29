using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Common.ValueObjects;

namespace Resto.Application.CashRegister.CloseShift;

public sealed record CloseCashRegisterShiftCommand(
    Guid ShiftId,
    Guid ClosedByUserId,
    decimal ClosingCashCounted
) : IRequest<Result<Guid>>;

public sealed class CloseCashRegisterShiftCommandHandler
    : IRequestHandler<CloseCashRegisterShiftCommand, Result<Guid>>
{
    private readonly ICashRegisterShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseCashRegisterShiftCommandHandler(
        ICashRegisterShiftRepository shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CloseCashRegisterShiftCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
            return Result<Guid>.Failure("Turno de caja no encontrado.");

        shift.Close(request.ClosedByUserId, Money.Create(request.ClosingCashCounted));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(shift.Id);
    }
}
