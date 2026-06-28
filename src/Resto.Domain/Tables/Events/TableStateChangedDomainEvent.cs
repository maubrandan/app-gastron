using Resto.Domain.Common;

namespace Resto.Domain.Tables.Events;

public sealed record TableStateChangedDomainEvent(int TableNumber, TableStatus Status) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
