using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Interfaces;
using Resto.Domain.Orders;
using Resto.Infrastructure.Persistence;

namespace Resto.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly RestoDbContext _context;

    public OrderRepository(RestoDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetActiveByTableNumberAsync(int tableNumber, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.Lines)
            .Where(o => o.TableNumber == tableNumber && o.Status != OrderStatus.Cerrado)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await _context.Orders.AddAsync(order, cancellationToken);

    public void RegisterNewLine(OrderLine line) =>
        _context.Entry(line).State = EntityState.Added;
}
