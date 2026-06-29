using Resto.Domain.CashRegister;
using Resto.Domain.Payments;

namespace Resto.Application.Common.Interfaces;

public interface ICashRegisterShiftRepository
{
    Task<CashRegisterShift?> GetOpenShiftAsync(CancellationToken cancellationToken = default);

    Task<CashRegisterShift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(CashRegisterShift shift, CancellationToken cancellationToken = default);
}

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
