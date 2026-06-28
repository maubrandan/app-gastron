using MediatR;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Products.Queries;

public sealed record GetProductsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IRestoReadDb _readDb;

    public GetProductsQueryHandler(IRestoReadDb readDb)
    {
        _readDb = readDb;
    }

    public Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken) =>
        _readDb.GetProductsAsync(request.IncludeInactive, cancellationToken);
}
