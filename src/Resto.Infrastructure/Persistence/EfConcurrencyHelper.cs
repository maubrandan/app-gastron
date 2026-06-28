using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Interfaces;
using Resto.Infrastructure.Persistence;

namespace Resto.Infrastructure.Persistence;

public sealed class EfConcurrencyHelper : IEfConcurrencyHelper
{
    private readonly RestoDbContext _context;

    public EfConcurrencyHelper(RestoDbContext context)
    {
        _context = context;
    }

    public void StampRowVersion<TEntity>(TEntity entity, byte[] rowVersion) where TEntity : class
    {
        var entry = _context.Entry(entity);
        var property = entry.Property("RowVersion");
        property.OriginalValue = rowVersion;
        property.CurrentValue = rowVersion;
        property.IsModified = false;
    }
}
