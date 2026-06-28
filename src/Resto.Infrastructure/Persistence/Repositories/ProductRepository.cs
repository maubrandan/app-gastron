using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Interfaces;
using Resto.Domain.Products;
using Resto.Infrastructure.Persistence;

namespace Resto.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly RestoDbContext _context;

    public ProductRepository(RestoDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        _context.Products.AddAsync(product, cancellationToken).AsTask();
}
