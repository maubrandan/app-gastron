using Resto.Domain.Exceptions;

namespace Resto.Domain.Common.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "ARS")
    {
        if (amount < 0)
            throw new DomainException("El importe no puede ser negativo.");

        return new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero), currency);
    }

    public static Money Zero(string currency = "ARS") => Create(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Create(Amount + other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("No se pueden operar importes con distinta moneda.");
    }

    public bool Equals(Money? other) =>
        other is not null &&
        Amount == other.Amount &&
        string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is Money money && Equals(money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency.ToUpperInvariant());
}
