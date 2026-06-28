using Resto.Application.Common.Interfaces;
using Resto.Domain.Common;
using Resto.Domain.Orders.Events;
using Resto.Domain.Tables.Events;

namespace Resto.Application.Common.Interfaces;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface IOrderSentToKitchenEventHandler
    : IDomainEventHandler<OrderSentToKitchenDomainEvent>;

public interface IOrderClosedEventHandler
    : IDomainEventHandler<OrderClosedDomainEvent>;

public interface ITableStateChangedEventHandler
    : IDomainEventHandler<TableStateChangedDomainEvent>;
