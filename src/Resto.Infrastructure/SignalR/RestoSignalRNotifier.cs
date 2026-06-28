using Microsoft.AspNetCore.SignalR;
using Resto.Application.Common.Interfaces;
using Resto.Infrastructure.SignalR;

namespace Resto.Infrastructure.SignalR;

public sealed class RestoSignalRNotifier : IRestoNotifier
{
    private const string KitchenGroup = "Cocina";
    private const string SalonGroup = "Salon";

    private readonly IHubContext<RestoHub, IRestoClient> _hubContext;

    public RestoSignalRNotifier(IHubContext<RestoHub, IRestoClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyKitchenOrderAddedAsync(KitchenOrderDto order, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(KitchenGroup).KitchenOrderAdded(order);

    public Task NotifyKitchenOrderRemovedAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(KitchenGroup).KitchenOrderRemoved(orderId);

    public Task NotifyTableStateUpdatedAsync(TableDto table, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(SalonGroup).TableStateUpdated(table);
}
