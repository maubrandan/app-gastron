using FluentValidation;
using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Exceptions;
using Resto.Domain.Products;

namespace Resto.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    decimal Price,
    string Category) : IRequest<Result<Guid>>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .WithMessage("El nombre es obligatorio (máx. 150 caracteres).");

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("La categoría es obligatoria (máx. 50 caracteres).");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("El precio debe ser mayor a cero.");
    }
}

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = Product.Create(request.Name, request.Price, request.Category);
            await _productRepository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(product.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
