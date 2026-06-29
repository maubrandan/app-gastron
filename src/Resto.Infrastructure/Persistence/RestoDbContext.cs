using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Resto.Domain.CashRegister;
using Resto.Domain.Orders;
using Resto.Domain.Payments;
using Resto.Domain.Products;
using Resto.Domain.Tables;
using Resto.Infrastructure.Identity;

namespace Resto.Infrastructure.Persistence;

public sealed class RestoDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public RestoDbContext(DbContextOptions<RestoDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CashRegisterShift> CashRegisterShifts => Set<CashRegisterShift>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestoDbContext).Assembly);
    }
}
