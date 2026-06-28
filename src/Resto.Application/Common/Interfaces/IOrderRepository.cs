using Resto.Domain.Orders;

namespace Resto.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetActiveByTableNumberAsync(int tableNumber, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    void RegisterNewLine(OrderLine line);
}
