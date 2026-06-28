using Resto.Application.Common;

namespace Resto.Application.Common.Helpers;

public static class BusinessDateHelper
{
    public static (DateTime StartUtc, DateTime EndUtc) GetUtcRangeForLocalDate(
        DateOnly localDate,
        string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStart = localDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);
        return (startUtc, endUtc);
    }
}
