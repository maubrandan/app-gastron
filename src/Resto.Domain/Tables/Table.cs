using Resto.Domain.Common;
using Resto.Domain.Exceptions;
using Resto.Domain.Tables.Events;

namespace Resto.Domain.Tables;

public sealed class Table : AggregateRoot
{
    public int Number { get; private set; }
    public TableStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Table() { }

    private Table(Guid id, int number) : base(id)
    {
        Number = number;
        Status = TableStatus.Libre;
    }

    public static Table Create(int number) => new(Guid.NewGuid(), number);

    public void Occupy()
    {
        if (Status != TableStatus.Libre)
            throw new DomainException("La mesa no está libre para abrir un pedido.");

        Status = TableStatus.Atendiendo;
        AddDomainEvent(new TableStateChangedDomainEvent(Number, Status));
    }

    public void MarkWaitingForBill()
    {
        if (Status != TableStatus.Atendiendo)
            throw new DomainException("Solo se puede solicitar cuenta en mesas en atención.");

        Status = TableStatus.EsperandoCuenta;
        AddDomainEvent(new TableStateChangedDomainEvent(Number, Status));
    }

    public void Release()
    {
        if (Status == TableStatus.Libre)
            throw new DomainException("La mesa ya está libre.");

        Status = TableStatus.Libre;
        AddDomainEvent(new TableStateChangedDomainEvent(Number, Status));
    }
}
