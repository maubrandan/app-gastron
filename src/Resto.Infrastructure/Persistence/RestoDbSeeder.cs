using Microsoft.EntityFrameworkCore;
using Resto.Domain.Products;
using Resto.Domain.Tables;

namespace Resto.Infrastructure.Persistence;

public static class RestoDbSeeder
{
    public static async Task SeedAsync(RestoDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Tables.AnyAsync(cancellationToken))
            return;

        for (var number = 1; number <= 12; number++)
            await context.Tables.AddAsync(Table.Create(number), cancellationToken);

        var products = new (string Name, decimal Price, string Category)[]
        {
            ("Agua mineral", 1200m, "Bebidas"),
            ("Cerveza artesanal", 3500m, "Bebidas"),
            ("Milanesa con papas", 8900m, "Platos Principales"),
            ("Ensalada César", 6500m, "Platos Principales"),
            ("Flan casero", 4200m, "Postres")
        };

        foreach (var (name, price, category) in products)
            await context.Products.AddAsync(Product.Create(name, price, category), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
