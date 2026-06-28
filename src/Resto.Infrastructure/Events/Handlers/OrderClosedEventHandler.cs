using Resto.Application.Common.Interfaces;
using Resto.Domain.Orders.Events;

namespace Resto.Infrastructure.Events.Handlers;

public sealed class OrderClosedEventHandler : IOrderClosedEventHandler
{
    private readonly IRestoReadDb _readDb;
    private readonly IRestoNotifier _notifier;

    public OrderClosedEventHandler(IRestoReadDb readDb, IRestoNotifier notifier)
    {
        _readDb = readDb;
        _notifier = notifier;
    }

    public async Task HandleAsync(OrderClosedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _notifier.NotifyKitchenOrderRemovedAsync(domainEvent.OrderId, cancellationToken);

        var tables = await _readDb.GetTablesAsync(cancellationToken);
        var table = tables.FirstOrDefault(t => t.Number == domainEvent.TableNumber);
        if (table is not null)
            await _notifier.NotifyTableStateUpdatedAsync(table, cancellationToken);
    }
}
