using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Api.Infrastructure;
using Resto.Application.Products.CreateProduct;
using Resto.Application.Products.DeactivateProduct;
using Resto.Application.Products.Queries;
using Resto.Application.Products.UpdateProduct;
using Resto.Infrastructure.Identity;

namespace Resto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (includeInactive && !User.IsInRole(AppRoles.Manager) && !User.IsInRole(AppRoles.Admin))
            return Forbid();

        var products = await _sender.Send(new GetProductsQuery(includeInactive), cancellationToken);
        return Ok(products);
    }

    [Authorize(Policy = AuthPolicies.ManagerOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateProductCommand(request.Name, request.Price, request.Category),
            cancellationToken);

        if (!result.IsSuccess)
            return this.BusinessError(result.Error!);

        return Ok(new { productId = result.Value });
    }

    [Authorize(Policy = AuthPolicies.ManagerOnly)]
    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateProductCommand(productId, request.Name, request.Price, request.Category),
            cancellationToken);

        if (!result.IsSuccess)
            return this.BusinessError(result.Error!);

        return Ok(new { productId = result.Value });
    }

    [Authorize(Policy = AuthPolicies.ManagerOnly)]
    [HttpPost("{productId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateProductCommand(productId), cancellationToken);

        if (!result.IsSuccess)
            return this.BusinessError(result.Error!);

        return Ok(new { productId = result.Value });
    }
}

public sealed record CreateProductRequest(string Name, decimal Price, string Category);

public sealed record UpdateProductRequest(string Name, decimal Price, string Category);
