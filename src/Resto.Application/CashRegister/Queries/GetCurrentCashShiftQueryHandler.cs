using MediatR;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.CashRegister.Queries;

public sealed record GetCurrentCashShiftQuery : IRequest<CashShiftDetailDto?>;

public sealed class GetCurrentCashShiftQueryHandler
    : IRequestHandler<GetCurrentCashShiftQuery, CashShiftDetailDto?>
{
    private readonly IRestoReadDb _readDb;

    public GetCurrentCashShiftQueryHandler(IRestoReadDb readDb) => _readDb = readDb;

    public Task<CashShiftDetailDto?> Handle(
        GetCurrentCashShiftQuery request,
        CancellationToken cancellationToken) =>
        _readDb.GetCurrentCashShiftAsync(cancellationToken);
}
