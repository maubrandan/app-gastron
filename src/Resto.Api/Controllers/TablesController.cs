using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Application.Tables.Queries;

namespace Resto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tables")]
public sealed class TablesController : ControllerBase
{
    private readonly ISender _sender;

    public TablesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tables = await _sender.Send(new GetTablesQuery(), cancellationToken);
        return Ok(tables);
    }
}
