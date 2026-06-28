using FluentAssertions;
using Resto.Domain.Common.ValueObjects;
using Resto.Domain.Exceptions;
using Resto.Domain.Products;

namespace Resto.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Deactivate_WhenActive_SetsInactive()
    {
        var product = Product.Create("Agua", 100m, "Bebidas");

        product.Deactivate();

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var product = Product.Create("Agua", 100m, "Bebidas");
        product.Deactivate();

        var act = () => product.Deactivate();

        act.Should().Throw<DomainException>()
            .WithMessage("El producto ya está inactivo.");
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesFields()
    {
        var product = Product.Create("Agua", 100m, "Bebidas");

        product.UpdateDetails("Agua mineral", 150m, "Bebidas");

        product.Name.Should().Be("Agua mineral");
        product.Price.Amount.Should().Be(150m);
        product.Category.Should().Be("Bebidas");
    }
}
