using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resto.Domain.Orders;
using Resto.Infrastructure.Persistence.Converters;

namespace Resto.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId)
            .IsRequired();

        builder.Property(l => l.ProductId)
            .IsRequired();

        builder.Property(l => l.Quantity)
            .HasColumnName("Quantity")
            .HasQuantityConversion()
            .IsRequired();

        builder.Property(l => l.UnitPrice)
            .HasColumnName("UnitPrice")
            .HasPrecision(18, 2)
            .HasMoneyConversion()
            .IsRequired();

        builder.Property(l => l.Subtotal)
            .HasColumnName("Subtotal")
            .HasPrecision(18, 2)
            .HasMoneyConversion()
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasMaxLength(250);
    }
}
