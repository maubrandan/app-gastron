using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resto.Domain.CashRegister;
using Resto.Infrastructure.Persistence.Converters;

namespace Resto.Infrastructure.Persistence.Configurations;

public sealed class CashRegisterShiftConfiguration : IEntityTypeConfiguration<CashRegisterShift>
{
    public void Configure(EntityTypeBuilder<CashRegisterShift> builder)
    {
        builder.ToTable("CashRegisterShifts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OpenedAt)
            .IsRequired();

        builder.Property(s => s.OpenedByUserId)
            .IsRequired();

        builder.Property(s => s.OpeningFloat)
            .HasPrecision(18, 2)
            .HasMoneyConversion()
            .IsRequired();

        builder.Property(s => s.ClosedAt);

        builder.Property(s => s.ClosedByUserId);

        builder.Property(s => s.ClosingCashCounted)
            .HasPrecision(18, 2)
            .HasConversion(
                m => m == null ? (decimal?)null : m.Amount,
                v => v == null ? null : Resto.Domain.Common.ValueObjects.Money.Create(v.Value));

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_CashRegisterShifts_Open")
            .IsUnique()
            .HasFilter("[Status] = 'Open'");
    }
}
