using MediatR;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Orders.Queries;

public sealed record GetOrderByTableQuery(int TableNumber) : IRequest<OrderDetailDto?>;

public sealed class GetOrderByTableQueryHandler
    : IRequestHandler<GetOrderByTableQuery, OrderDetailDto?>
{
    private readonly IRestoReadDb _readDb;

    public GetOrderByTableQueryHandler(IRestoReadDb readDb)
    {
        _readDb = readDb;
    }

    public Task<OrderDetailDto?> Handle(
        GetOrderByTableQuery request,
        CancellationToken cancellationToken) =>
        _readDb.GetActiveOrderByTableAsync(request.TableNumber, cancellationToken);
}
