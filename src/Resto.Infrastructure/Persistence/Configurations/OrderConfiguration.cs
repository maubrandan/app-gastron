using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resto.Domain.Orders;
using Resto.Infrastructure.Persistence.Converters;

namespace Resto.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TableNumber)
            .IsRequired();

        builder.HasIndex(o => o.TableNumber);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.CreatedByWaiterId)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.SentToKitchenAt);

        builder.Property(o => o.ClosedAt);

        builder.Property(o => o.ClosedByUserId);

        builder.Property(o => o.Total)
            .HasColumnName("Total")
            .HasPrecision(18, 2)
            .HasMoneyConversion()
            .IsRequired();

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status_Kitchen")
            .IncludeProperties(o => new { o.TableNumber, o.SentToKitchenAt });

        builder.HasIndex(o => o.ClosedAt)
            .HasDatabaseName("IX_Orders_ClosedAt")
            .HasFilter("[Status] = 'Cerrado'");
    }
}
