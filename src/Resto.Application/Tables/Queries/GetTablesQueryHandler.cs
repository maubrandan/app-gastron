using MediatR;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Tables.Queries;

public sealed record GetTablesQuery : IRequest<IReadOnlyList<TableDto>>;

public sealed class GetTablesQueryHandler
    : IRequestHandler<GetTablesQuery, IReadOnlyList<TableDto>>
{
    private readonly IRestoReadDb _readDb;

    public GetTablesQueryHandler(IRestoReadDb readDb)
    {
        _readDb = readDb;
    }

    public Task<IReadOnlyList<TableDto>> Handle(
        GetTablesQuery request,
        CancellationToken cancellationToken) =>
        _readDb.GetTablesAsync(cancellationToken);
}
