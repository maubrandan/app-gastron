using Resto.Domain.Common;

namespace Resto.Domain.Orders.Events;

public sealed record OrderSentToKitchenDomainEvent(
    Guid OrderId,
    int TableNumber,
    DateTime SentToKitchenAt) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
