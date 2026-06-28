using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Interfaces;
using Resto.Domain.Tables;
using Resto.Infrastructure.Persistence;

namespace Resto.Infrastructure.Persistence.Repositories;

public sealed class TableRepository : ITableRepository
{
    private readonly RestoDbContext _context;

    public TableRepository(RestoDbContext context)
    {
        _context = context;
    }

    public Task<Table?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
        _context.Tables.FirstOrDefaultAsync(t => t.Number == number, cancellationToken);

    public async Task<IReadOnlyList<Table>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Tables
            .AsNoTracking()
            .OrderBy(t => t.Number)
            .ToListAsync(cancellationToken);
}
