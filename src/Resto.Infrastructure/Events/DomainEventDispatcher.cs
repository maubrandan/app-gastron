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
            var eventType = domainEvent.GetType().Name;

            try
            {
                await EventDispatchRetry.ExecuteAsync(
                    ct => DispatchSingleAsync(domainEvent, ct),
                    _logger,
                    eventType,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al despachar evento {EventType} tras agotar reintentos",
                    eventType);
            }
        }
    }

    private Task DispatchSingleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        domainEvent switch
        {
            OrderSentToKitchenDomainEvent sent => _orderSentHandler.HandleAsync(sent, cancellationToken),
            OrderClosedDomainEvent closed => _orderClosedHandler.HandleAsync(closed, cancellationToken),
            TableStateChangedDomainEvent tableChanged => _tableStateHandler.HandleAsync(tableChanged, cancellationToken),
            _ => LogUnhandledAsync(domainEvent),
        };

    private Task LogUnhandledAsync(IDomainEvent domainEvent)
    {
        _logger.LogWarning("Evento de dominio no manejado: {EventType}", domainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}
