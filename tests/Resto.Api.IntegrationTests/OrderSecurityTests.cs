using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Resto.Infrastructure.Identity;

namespace Resto.Api.IntegrationTests;

public sealed class OrderSecurityTests : IClassFixture<RestoApiFactory>
{
    private readonly RestoApiClient _api;

    public OrderSecurityTests(RestoApiFactory factory)
    {
        _api = new RestoApiClient(factory.CreateClient());
    }

    [Fact]
    public async Task ConfirmForKitchen_AsWaiter_ReturnsForbidden()
    {
        var waiterToken = await _api.LoginAsync("mozo1@resto.local", IdentitySeeder.DemoPassword);
        _api.SetBearerToken(waiterToken);

        var tables = await _api.GetAsync<RestoApiClient.TableResponse[]>("/api/tables");
        var table = tables.First(t => t.Status == "Libre");

        var createResponse = await _api.PostAsync<RestoApiClient.CreateOrderResponse>(
            "/api/orders",
            new { tableNumber = table.Number, tableRowVersion = table.RowVersion });

        var products = await _api.GetAsync<RestoApiClient.ProductResponse[]>("/api/products");
        var product = products.First(p => p.IsActive);

        var lineResponse = await _api.PostAsync<RestoApiClient.RowVersionResponse>(
            $"/api/orders/{createResponse.OrderId}/lines",
            new
            {
                productId = product.Id,
                quantity = 1,
                notes = (string?)null,
                rowVersion = createResponse.OrderRowVersion,
            });

        var response = await _api.PostRawAsync(
            $"/api/orders/{createResponse.OrderId}/confirm-for-kitchen",
            new { rowVersion = lineResponse.RowVersion });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddOrderLine_WithStaleRowVersion_ReturnsConflictProblemDetails()
    {
        var waiterToken = await _api.LoginAsync("mozo1@resto.local", IdentitySeeder.DemoPassword);
        _api.SetBearerToken(waiterToken);

        var tables = await _api.GetAsync<RestoApiClient.TableResponse[]>("/api/tables");
        var table = tables.First(t => t.Status == "Libre");

        var createResponse = await _api.PostAsync<RestoApiClient.CreateOrderResponse>(
            "/api/orders",
            new { tableNumber = table.Number, tableRowVersion = table.RowVersion });

        var products = await _api.GetAsync<RestoApiClient.ProductResponse[]>("/api/products");
        var product = products.First(p => p.IsActive);

        await _api.PostAsync<RestoApiClient.RowVersionResponse>(
            $"/api/orders/{createResponse.OrderId}/lines",
            new
            {
                productId = product.Id,
                quantity = 1,
                notes = (string?)null,
                rowVersion = createResponse.OrderRowVersion,
            });

        var response = await _api.PostRawAsync(
            $"/api/orders/{createResponse.OrderId}/lines",
            new
            {
                productId = product.Id,
                quantity = 1,
                notes = (string?)null,
                rowVersion = createResponse.OrderRowVersion,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<RestoApiClient.ProblemResponse>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
        problem.Detail.Should().Contain("modificó");
        problem.Type.Should().Be("https://resto.local/errors/concurrency-conflict");
    }
}
