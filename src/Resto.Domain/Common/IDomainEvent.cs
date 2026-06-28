namespace Resto.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
