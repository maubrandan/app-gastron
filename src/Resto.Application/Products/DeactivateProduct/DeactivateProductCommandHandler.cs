using FluentValidation;
using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;
using Resto.Domain.Exceptions;

namespace Resto.Application.Products.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<Result<Guid>>;

public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("El identificador del producto es obligatorio.");
    }
}

public sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<Guid>.Failure("Producto no encontrado.");

        try
        {
            product.Deactivate();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(product.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
