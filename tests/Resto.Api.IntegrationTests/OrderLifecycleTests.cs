using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Resto.Infrastructure.Identity;

namespace Resto.Api.IntegrationTests;

public sealed class OrderLifecycleTests : IClassFixture<RestoApiFactory>
{
    private readonly RestoApiClient _api;

    public OrderLifecycleTests(RestoApiFactory factory)
    {
        _api = new RestoApiClient(factory.CreateClient());
    }

    [Fact]
    public async Task WaiterToManagerFlow_CreateAddConfirmClose_CompletesSuccessfully()
    {
        var waiterToken = await _api.LoginAsync("mozo1@resto.local", IdentitySeeder.DemoPassword);
        var managerToken = await _api.LoginAsync("encargado@resto.local", IdentitySeeder.DemoPassword);

        _api.SetBearerToken(waiterToken);

        var tables = await _api.GetAsync<RestoApiClient.TableResponse[]>("/api/tables");
        var table = tables.First(t => t.Status == "Libre");
        table.Number.Should().BeGreaterThan(0);

        var createResponse = await _api.PostAsync<RestoApiClient.CreateOrderResponse>(
            "/api/orders",
            new { tableNumber = table.Number, tableRowVersion = table.RowVersion });

        createResponse.OrderId.Should().NotBeEmpty();

        var products = await _api.GetAsync<RestoApiClient.ProductResponse[]>("/api/products");
        var product = products.First(p => p.IsActive);

        var lineResponse = await _api.PostAsync<RestoApiClient.RowVersionResponse>(
            $"/api/orders/{createResponse.OrderId}/lines",
            new
            {
                productId = product.Id,
                quantity = 2,
                notes = "Sin hielo",
                rowVersion = createResponse.OrderRowVersion,
            });

        lineResponse.RowVersion.Should().NotBeNullOrWhiteSpace();

        _api.SetBearerToken(managerToken);

        await _api.PostAsync<object>(
            $"/api/orders/{createResponse.OrderId}/confirm-for-kitchen",
            new { rowVersion = lineResponse.RowVersion });

        var kitchenOrders = await _api.GetAsync<RestoApiClient.KitchenOrderResponse[]>("/api/kitchen/active-orders");
        kitchenOrders.Should().Contain(o => o.Id == createResponse.OrderId);

        var tablesAfterConfirm = await _api.GetAsync<RestoApiClient.TableResponse[]>("/api/tables");
        var occupiedTable = tablesAfterConfirm.Single(t => t.Number == table.Number);
        occupiedTable.Status.Should().BeOneOf("Atendiendo", "EsperandoCuenta");

        var orderBeforeClose = await _api.GetAsync<RestoApiClient.OrderResponse>($"/api/orders/{createResponse.OrderId}");
        orderBeforeClose.Status.Should().Be("ConfirmadoEnCocina");

        await _api.PostAsync<object>(
            "/api/cash-register/shifts/open",
            new { openingFloat = 5000m });

        await _api.PostAsync<object>(
            $"/api/orders/{createResponse.OrderId}/close-and-bill",
            new
            {
                orderRowVersion = orderBeforeClose.RowVersion,
                tableRowVersion = occupiedTable.RowVersion,
                paymentMethod = "Cash",
            });

        var tablesAfterClose = await _api.GetAsync<RestoApiClient.TableResponse[]>("/api/tables");
        tablesAfterClose.Single(t => t.Number == table.Number).Status.Should().Be("Libre");

        var closedOrder = await _api.GetAsync<RestoApiClient.OrderResponse>($"/api/orders/{createResponse.OrderId}");
        closedOrder.Status.Should().Be("Cerrado");
        closedOrder.ClosedAt.Should().NotBeNull();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await _api.GetAsync<RestoApiClient.DailySummaryResponse>(
            $"/api/reports/daily-summary?date={today:yyyy-MM-dd}");
        summary.OrderCount.Should().BeGreaterThanOrEqualTo(1);
        summary.TotalRevenue.Should().BeGreaterThan(0);
    }
}
