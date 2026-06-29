using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resto.Domain.Payments;
using Resto.Infrastructure.Persistence.Converters;

namespace Resto.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId)
            .IsRequired();

        builder.HasIndex(p => p.OrderId)
            .IsUnique();

        builder.Property(p => p.CashRegisterShiftId)
            .IsRequired();

        builder.HasIndex(p => p.CashRegisterShiftId);

        builder.Property(p => p.Method)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .HasMoneyConversion()
            .IsRequired();

        builder.Property(p => p.PaidAt)
            .IsRequired();

        builder.Property(p => p.RegisteredByUserId)
            .IsRequired();
    }
}
