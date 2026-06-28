using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Resto.Application.Common.Interfaces;
using Resto.Domain.Common;
using Resto.Domain.Exceptions;

namespace Resto.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly RestoDbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;

    public UnitOfWork(RestoDbContext context, IDomainEventDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        try
        {
            PreserveConcurrencyTokens(_context.ChangeTracker.Entries());
            var result = await _context.SaveChangesAsync(cancellationToken);

            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);

            foreach (var entry in _context.ChangeTracker.Entries<AggregateRoot>())
                entry.Entity.ClearDomainEvents();

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "Otro usuario modificó este registro. Recargá e intentá de nuevo.");
        }
    }

    private static void PreserveConcurrencyTokens(IEnumerable<EntityEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
                continue;

            foreach (var property in entry.Properties.Where(p => p.Metadata.IsConcurrencyToken))
                property.IsModified = false;
        }
    }
}
