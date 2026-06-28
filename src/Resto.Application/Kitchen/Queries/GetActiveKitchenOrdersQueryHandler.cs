using MediatR;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Kitchen.Queries;

public sealed record GetActiveKitchenOrdersQuery(string? Category = null) : IRequest<IReadOnlyList<KitchenOrderDto>>;

public sealed class GetActiveKitchenOrdersQueryHandler
    : IRequestHandler<GetActiveKitchenOrdersQuery, IReadOnlyList<KitchenOrderDto>>
{
    private readonly IRestoReadDb _readDb;

    public GetActiveKitchenOrdersQueryHandler(IRestoReadDb readDb)
    {
        _readDb = readDb;
    }

    public Task<IReadOnlyList<KitchenOrderDto>> Handle(
        GetActiveKitchenOrdersQuery request,
        CancellationToken cancellationToken) =>
        _readDb.GetActiveKitchenOrdersAsync(request.Category, cancellationToken);
}
