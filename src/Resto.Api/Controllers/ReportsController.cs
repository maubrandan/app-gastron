using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Application.Reports.Queries;
using Resto.Infrastructure.Identity;

namespace Resto.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.ManagerOnly)]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("daily-summary")]
    public async Task<IActionResult> GetDailySummary(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var summary = await _sender.Send(new GetDailySummaryQuery(date), cancellationToken);
        return Ok(summary);
    }

    [HttpGet("closed-orders")]
    public async Task<IActionResult> GetClosedOrders(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var orders = await _sender.Send(new GetClosedOrdersQuery(date), cancellationToken);
        return Ok(orders);
    }
}
