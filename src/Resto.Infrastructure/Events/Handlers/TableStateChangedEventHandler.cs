using Resto.Application.Common.Interfaces;
using Resto.Domain.Tables.Events;

namespace Resto.Infrastructure.Events.Handlers;

public sealed class TableStateChangedEventHandler : ITableStateChangedEventHandler
{
    private readonly IRestoReadDb _readDb;
    private readonly IRestoNotifier _notifier;

    public TableStateChangedEventHandler(IRestoReadDb readDb, IRestoNotifier notifier)
    {
        _readDb = readDb;
        _notifier = notifier;
    }

    public async Task HandleAsync(TableStateChangedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var tables = await _readDb.GetTablesAsync(cancellationToken);
        var table = tables.FirstOrDefault(t => t.Number == domainEvent.TableNumber);
        if (table is not null)
            await _notifier.NotifyTableStateUpdatedAsync(table, cancellationToken);
    }
}
