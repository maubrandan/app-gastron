using MediatR;
using Microsoft.Extensions.Options;
using Resto.Application.Common;
using Resto.Application.Common.Helpers;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Reports.Queries;

public sealed record GetClosedOrdersQuery(DateOnly Date) : IRequest<IReadOnlyList<ClosedOrderSummaryDto>>;

public sealed class GetClosedOrdersQueryHandler : IRequestHandler<GetClosedOrdersQuery, IReadOnlyList<ClosedOrderSummaryDto>>
{
    private readonly IRestoReadDb _readDb;
    private readonly BusinessSettings _settings;

    public GetClosedOrdersQueryHandler(IRestoReadDb readDb, IOptions<BusinessSettings> settings)
    {
        _readDb = readDb;
        _settings = settings.Value;
    }

    public Task<IReadOnlyList<ClosedOrderSummaryDto>> Handle(
        GetClosedOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = BusinessDateHelper.GetUtcRangeForLocalDate(request.Date, _settings.TimeZoneId);
        return _readDb.GetClosedOrdersAsync(startUtc, endUtc, cancellationToken);
    }
}
