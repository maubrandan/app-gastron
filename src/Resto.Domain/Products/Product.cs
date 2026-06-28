using Resto.Domain.Common;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;

namespace Resto.Domain.Products;

public sealed class Product : Entity
{
    public string Name { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero();
    public string Category { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Product() { }

    public Product(Guid id, string name, Money price, string category) : base(id)
    {
        Name = name;
        Price = price;
        Category = category;
        IsActive = true;
    }

    public static Product Create(string name, decimal price, string category)
    {
        ValidateDetails(name, price, category);
        return new(Guid.NewGuid(), name.Trim(), Money.Create(price), category.Trim());
    }

    public void UpdateDetails(string name, decimal price, string category)
    {
        ValidateDetails(name, price, category);
        Name = name.Trim();
        Price = Money.Create(price);
        Category = category.Trim();
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("El producto ya está inactivo.");

        IsActive = false;
    }

    private static void ValidateDetails(string name, decimal price, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del producto es obligatorio.");

        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("La categoría del producto es obligatoria.");

        if (price <= 0)
            throw new DomainException("El precio del producto debe ser mayor a cero.");
    }
}
