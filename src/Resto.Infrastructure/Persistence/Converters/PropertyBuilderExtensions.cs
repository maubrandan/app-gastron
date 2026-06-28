using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resto.Domain.Common.ValueObjects;

namespace Resto.Infrastructure.Persistence.Converters;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<Money> HasMoneyConversion(this PropertyBuilder<Money> builder) =>
        builder.HasConversion(
            money => money.Amount,
            amount => Money.Create(amount));

    public static PropertyBuilder<Quantity> HasQuantityConversion(this PropertyBuilder<Quantity> builder) =>
        builder.HasConversion(
            quantity => quantity.Value,
            value => Quantity.Create(value));
}
