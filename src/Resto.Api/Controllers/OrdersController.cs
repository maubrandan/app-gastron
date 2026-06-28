using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Application.Common.Helpers;
using Resto.Application.Common.Interfaces;
using Resto.Application.Orders.AddOrderLine;
using Resto.Application.Orders.CloseAndBill;
using Resto.Application.Orders.ConfirmOrderForKitchen;
using Resto.Application.Orders.CreateOrder;
using Resto.Application.Orders.Queries;
using Resto.Application.Orders.RemoveOrderLine;
using Resto.Application.Orders.RequestBill;
using Resto.Domain.Exceptions;
using Resto.Infrastructure.Identity;

namespace Resto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpGet("by-table/{tableNumber:int}")]
    public async Task<IActionResult> GetByTable(int tableNumber, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(new GetOrderByTableQuery(tableNumber), cancellationToken);
        return order is null ? NoContent() : Ok(order);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(orderId), cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [Authorize(Policy = AuthPolicies.WaiterOrManager)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } waiterId)
            return Unauthorized();

        var result = await _sender.Send(
            new CreateOrderCommand(
                request.TableNumber,
                waiterId,
                RowVersionHelper.FromBase64(request.TableRowVersion)),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [Authorize(Policy = AuthPolicies.WaiterOrManager)]
    [HttpPost("{orderId:guid}/lines")]
    public async Task<IActionResult> AddLine(
        Guid orderId,
        [FromBody] AddOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sender.Send(
                new AddOrderLineCommand(
                    orderId,
                    request.ProductId,
                    request.Quantity,
                    request.Notes,
                    RowVersionHelper.FromBase64(request.RowVersion)),
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(new { rowVersion = result.Value });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ConcurrencyConflictException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    [Authorize(Policy = AuthPolicies.WaiterOrManager)]
    [HttpDelete("{orderId:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(
        Guid orderId,
        Guid lineId,
        [FromBody] RowVersionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sender.Send(
                new RemoveOrderLineCommand(
                    orderId,
                    lineId,
                    RowVersionHelper.FromBase64(request.RowVersion)),
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(new { rowVersion = result.Value });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ConcurrencyConflictException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    [Authorize(Policy = AuthPolicies.ManagerOnly)]
    [HttpPost("{orderId:guid}/confirm-for-kitchen")]
    public async Task<IActionResult> ConfirmForKitchen(
        Guid orderId,
        [FromBody] RowVersionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sender.Send(
                new ConfirmOrderForKitchenCommand(
                    orderId,
                    RowVersionHelper.FromBase64(request.RowVersion)),
                cancellationToken);

            if (!result.IsSuccess)
                return NotFound(new { error = result.Error });

            return Ok(new { orderId = result.Value });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ConcurrencyConflictException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    [Authorize(Policy = AuthPolicies.ManagerOnly)]
    [HttpPost("{orderId:guid}/close-and-bill")]
    public async Task<IActionResult> CloseAndBill(
        Guid orderId,
        [FromBody] CloseAndBillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_currentUser.UserId is not { } userId)
                return Unauthorized();

            var result = await _sender.Send(
                new CloseAndBillOrderCommand(
                    orderId,
                    RowVersionHelper.FromBase64(request.OrderRowVersion),
                    RowVersionHelper.FromBase64(request.TableRowVersion),
                    userId),
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(new { orderId = result.Value });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ConcurrencyConflictException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    [Authorize(Policy = AuthPolicies.WaiterOrManager)]
    [HttpPost("{orderId:guid}/request-bill")]
    public async Task<IActionResult> RequestBill(
        Guid orderId,
        [FromBody] RequestBillRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sender.Send(
                new RequestBillCommand(
                    orderId,
                    RowVersionHelper.FromBase64(request.OrderRowVersion),
                    RowVersionHelper.FromBase64(request.TableRowVersion)),
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(new { orderId = result.Value });
        }
        catch (ConcurrencyConflictException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    private IActionResult ConflictProblem(string detail) =>
        Conflict(new
        {
            type = "https://resto.local/errors/concurrency-conflict",
            title = "Conflicto de concurrencia",
            status = 409,
            detail
        });
}

public sealed record CreateOrderRequest(int TableNumber, string TableRowVersion);

public sealed record AddOrderLineRequest(Guid ProductId, int Quantity, string? Notes, string RowVersion);

public sealed record RowVersionRequest(string RowVersion);

public sealed record CloseAndBillRequest(string OrderRowVersion, string TableRowVersion);

public sealed record RequestBillRequest(string OrderRowVersion, string TableRowVersion);
