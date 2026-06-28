using Resto.Domain.Exceptions;

namespace Resto.Domain.Common.ValueObjects;

public sealed class Quantity : IEquatable<Quantity>
{
    public int Value { get; }

    private Quantity(int value) => Value = value;

    public static Quantity Create(int value)
    {
        if (value <= 0)
            throw new DomainException("La cantidad debe ser un entero mayor a cero.");

        return new Quantity(value);
    }

    public bool Equals(Quantity? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Quantity quantity && Equals(quantity);

    public override int GetHashCode() => Value.GetHashCode();
}
