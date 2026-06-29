using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Api.Infrastructure;
using Resto.Application.CashRegister.CloseShift;
using Resto.Application.CashRegister.OpenShift;
using Resto.Application.CashRegister.Queries;
using Resto.Application.Common.Interfaces;
using Resto.Infrastructure.Identity;

namespace Resto.Api.Controllers;

[ApiController]
[Route("api/cash-register")]
[Authorize(Policy = AuthPolicies.ManagerOnly)]
public sealed class CashRegisterController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public CashRegisterController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpGet("shifts/current")]
    public async Task<IActionResult> GetCurrentShift(CancellationToken cancellationToken)
    {
        var shift = await _sender.Send(new GetCurrentCashShiftQuery(), cancellationToken);
        return shift is null ? NoContent() : Ok(shift);
    }

    [HttpPost("shifts/open")]
    public async Task<IActionResult> OpenShift(
        [FromBody] OpenShiftRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return this.UnauthorizedError("Sesión inválida.");

        var result = await _sender.Send(
            new OpenCashRegisterShiftCommand(userId, request.OpeningFloat),
            cancellationToken);

        if (!result.IsSuccess)
            return this.BusinessError(result.Error!);

        return Ok(new { shiftId = result.Value });
    }

    [HttpPost("shifts/{shiftId:guid}/close")]
    public async Task<IActionResult> CloseShift(
        Guid shiftId,
        [FromBody] CloseShiftRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return this.UnauthorizedError("Sesión inválida.");

        var result = await _sender.Send(
            new CloseCashRegisterShiftCommand(shiftId, userId, request.ClosingCashCounted),
            cancellationToken);

        if (!result.IsSuccess)
            return this.BusinessError(result.Error!);

        return Ok(new { shiftId = result.Value });
    }
}

public sealed record OpenShiftRequest(decimal OpeningFloat);

public sealed record CloseShiftRequest(decimal ClosingCashCounted);
