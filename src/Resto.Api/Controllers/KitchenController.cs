using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Application.Kitchen.Queries;
using Resto.Infrastructure.Identity;

namespace Resto.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.KitchenOrManager)]
[Route("api/kitchen")]
public sealed class KitchenController : ControllerBase
{
    private readonly ISender _sender;

    public KitchenController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("active-orders")]
    public async Task<IActionResult> GetActiveOrders(
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var orders = await _sender.Send(new GetActiveKitchenOrdersQuery(category), cancellationToken);
        return Ok(orders);
    }
}
