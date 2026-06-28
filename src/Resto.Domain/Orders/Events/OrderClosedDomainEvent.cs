using Resto.Domain.Common;

namespace Resto.Domain.Orders.Events;

public sealed record OrderClosedDomainEvent(Guid OrderId, int TableNumber) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
