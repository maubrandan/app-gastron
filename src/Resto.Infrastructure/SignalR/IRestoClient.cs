using Resto.Application.Common.Interfaces;

namespace Resto.Infrastructure.SignalR;

public interface IRestoClient
{
    Task KitchenOrderAdded(KitchenOrderDto order);

    Task KitchenOrderRemoved(Guid orderId);

    Task TableStateUpdated(TableDto table);
}
