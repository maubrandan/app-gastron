using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Interfaces;
using Resto.Domain.CashRegister;

namespace Resto.Infrastructure.Persistence.Repositories;

public sealed class CashRegisterShiftRepository : ICashRegisterShiftRepository
{
    private readonly RestoDbContext _context;

    public CashRegisterShiftRepository(RestoDbContext context) => _context = context;

    public Task<CashRegisterShift?> GetOpenShiftAsync(CancellationToken cancellationToken = default) =>
        _context.CashRegisterShifts
            .FirstOrDefaultAsync(s => s.Status == CashShiftStatus.Open, cancellationToken);

    public Task<CashRegisterShift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CashRegisterShifts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(CashRegisterShift shift, CancellationToken cancellationToken = default) =>
        await _context.CashRegisterShifts.AddAsync(shift, cancellationToken);
}
