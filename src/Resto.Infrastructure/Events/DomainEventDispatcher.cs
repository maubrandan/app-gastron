using Microsoft.Extensions.Logging;
using Resto.Application.Common.Interfaces;
using Resto.Domain.Common;
using Resto.Domain.Orders.Events;
using Resto.Domain.Tables.Events;

namespace Resto.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;
    private readonly IOrderSentToKitchenEventHandler _orderSentHandler;
    private readonly IOrderClosedEventHandler _orderClosedHandler;
    private readonly ITableStateChangedEventHandler _tableStateHandler;

    public DomainEventDispatcher(
        ILogger<DomainEventDispatcher> logger,
        IOrderSentToKitchenEventHandler orderSentHandler,
        IOrderClosedEventHandler orderClosedHandler,
        ITableStateChangedEventHandler tableStateHandler)
    {
        _logger = logger;
        _orderSentHandler = orderSentHandler;
        _orderClosedHandler = orderClosedHandler;
        _tableStateHandler = tableStateHandler;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            try
            {
                switch (domainEvent)
                {
                    case OrderSentToKitchenDomainEvent sent:
                        await _orderSentHandler.HandleAsync(sent, cancellationToken);
                        break;
                    case OrderClosedDomainEvent closed:
                        await _orderClosedHandler.HandleAsync(closed, cancellationToken);
                        break;
                    case TableStateChangedDomainEvent tableChanged:
                        await _tableStateHandler.HandleAsync(tableChanged, cancellationToken);
                        break;
                    default:
                        _logger.LogWarning("Evento de dominio no manejado: {EventType}", domainEvent.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al despachar evento {EventType}", domainEvent.GetType().Name);
            }
        }
    }
}
