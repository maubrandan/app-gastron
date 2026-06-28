using Resto.Application.Common.Interfaces;

namespace Resto.Application.Common.Interfaces;

public interface IRestoNotifier
{
    Task NotifyKitchenOrderAddedAsync(KitchenOrderDto order, CancellationToken cancellationToken = default);

    Task NotifyKitchenOrderRemovedAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task NotifyTableStateUpdatedAsync(TableDto table, CancellationToken cancellationToken = default);
}
