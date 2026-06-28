using MediatR;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto?>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto?>
{
    private readonly IRestoReadDb _readDb;

    public GetOrderByIdQueryHandler(IRestoReadDb readDb)
    {
        _readDb = readDb;
    }

    public Task<OrderDetailDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken) =>
        _readDb.GetOrderByIdAsync(request.OrderId, cancellationToken);
}
