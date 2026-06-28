using MediatR;
using Microsoft.Extensions.Options;
using Resto.Application.Common;
using Resto.Application.Common.Helpers;
using Resto.Application.Common.Interfaces;

namespace Resto.Application.Reports.Queries;

public sealed record GetDailySummaryQuery(DateOnly Date) : IRequest<DailySummaryDto>;

public sealed class GetDailySummaryQueryHandler : IRequestHandler<GetDailySummaryQuery, DailySummaryDto>
{
    private readonly IRestoReadDb _readDb;
    private readonly BusinessSettings _settings;

    public GetDailySummaryQueryHandler(IRestoReadDb readDb, IOptions<BusinessSettings> settings)
    {
        _readDb = readDb;
        _settings = settings.Value;
    }

    public Task<DailySummaryDto> Handle(GetDailySummaryQuery request, CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = BusinessDateHelper.GetUtcRangeForLocalDate(request.Date, _settings.TimeZoneId);
        return _readDb.GetDailySummaryAsync(startUtc, endUtc, _settings.TimeZoneId, cancellationToken);
    }
}
