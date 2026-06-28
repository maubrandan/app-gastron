using Resto.Application.Common.Interfaces;
using Resto.Domain.Orders.Events;

namespace Resto.Infrastructure.Events.Handlers;

public sealed class OrderSentToKitchenEventHandler : IOrderSentToKitchenEventHandler
{
    private readonly IRestoReadDb _readDb;
    private readonly IRestoNotifier _notifier;

    public OrderSentToKitchenEventHandler(IRestoReadDb readDb, IRestoNotifier notifier)
    {
        _readDb = readDb;
        _notifier = notifier;
    }

    public async Task HandleAsync(OrderSentToKitchenDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var order = await _readDb.GetKitchenOrderByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is not null)
            await _notifier.NotifyKitchenOrderAddedAsync(order, cancellationToken);
    }
}
