using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Helpers;
using Resto.Application.Common.Interfaces;
using Resto.Domain.Orders;
using Resto.Infrastructure.Persistence;

namespace Resto.Infrastructure.Persistence;

public sealed class RestoReadDb : IRestoReadDb
{
    private readonly RestoDbContext _context;

    public RestoReadDb(RestoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<KitchenOrderDto>> GetActiveKitchenOrdersAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.ConfirmadoEnCocina)
            .OrderBy(o => o.SentToKitchenAt)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var result = new List<KitchenOrderDto>();

        foreach (var orderId in orders)
        {
            var dto = await GetKitchenOrderByIdAsync(orderId, cancellationToken);
            if (dto is null)
                continue;

            var filtered = FilterKitchenOrderByCategory(dto, category);
            if (filtered is not null)
                result.Add(filtered);
        }

        return result;
    }

    public async Task<KitchenOrderDto?> GetKitchenOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.Status == OrderStatus.ConfirmadoEnCocina)
            .Select(o => new { o.Id, o.TableNumber, o.SentToKitchenAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null || order.SentToKitchenAt is null)
            return null;

        var lines = await BuildKitchenLinesAsync(orderId, cancellationToken);

        return new KitchenOrderDto(
            order.Id,
            order.TableNumber,
            order.SentToKitchenAt.Value,
            lines);
    }

    public async Task<IReadOnlyList<TableDto>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = await _context.Tables
            .AsNoTracking()
            .OrderBy(t => t.Number)
            .ToListAsync(cancellationToken);

        var activeOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatus.Cerrado)
            .Select(o => new { o.TableNumber, o.Id })
            .ToListAsync(cancellationToken);

        var orderByTable = activeOrders
            .GroupBy(o => o.TableNumber)
            .ToDictionary(g => g.Key, g => g.First().Id);

        return tables.Select(t => new TableDto(
            t.Number,
            t.Status.ToString(),
            RowVersionHelper.ToBase64(t.RowVersion),
            orderByTable.TryGetValue(t.Number, out var orderId) ? orderId : null)).ToList();
    }

    public async Task<OrderDetailDto?> GetActiveOrderByTableAsync(
        int tableNumber,
        CancellationToken cancellationToken = default)
    {
        var orderId = await _context.Orders
            .AsNoTracking()
            .Where(o => o.TableNumber == tableNumber && o.Status != OrderStatus.Cerrado)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return orderId == Guid.Empty
            ? null
            : await GetOrderByIdAsync(orderId, cancellationToken);
    }

    public async Task<OrderDetailDto?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new
            {
                o.Id,
                o.TableNumber,
                o.Status,
                o.Total,
                o.RowVersion,
                o.CreatedAt,
                o.SentToKitchenAt,
                o.ClosedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return null;

        var lines = await BuildOrderLinesAsync(orderId, cancellationToken);

        return new OrderDetailDto(
            order.Id,
            order.TableNumber,
            order.Status.ToString(),
            order.Total.Amount,
            RowVersionHelper.ToBase64(order.RowVersion),
            order.CreatedAt,
            order.SentToKitchenAt,
            order.ClosedAt,
            lines);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price.Amount, p.Category, p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<DailySummaryDto> GetDailySummaryAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var closedOrders = await _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Cerrado
                && o.ClosedAt >= rangeStartUtc
                && o.ClosedAt < rangeEndUtc)
            .Select(o => new { o.Total, o.ClosedAt })
            .ToListAsync(cancellationToken);

        var orderCount = closedOrders.Count;
        var totalRevenue = closedOrders.Sum(o => o.Total.Amount);
        var averageTicket = orderCount == 0 ? 0m : totalRevenue / orderCount;

        var ordersByHour = closedOrders
            .Where(o => o.ClosedAt.HasValue)
            .GroupBy(o => TimeZoneInfo.ConvertTimeFromUtc(o.ClosedAt!.Value, timeZone).Hour)
            .Select(g => new OrdersByHourDto(g.Key, g.Count(), g.Sum(x => x.Total.Amount)))
            .OrderBy(x => x.Hour)
            .ToList();

        return new DailySummaryDto(orderCount, totalRevenue, averageTicket, ordersByHour);
    }

    public async Task<IReadOnlyList<ClosedOrderSummaryDto>> GetClosedOrdersAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Cerrado
                && o.ClosedAt >= rangeStartUtc
                && o.ClosedAt < rangeEndUtc)
            .OrderByDescending(o => o.ClosedAt)
            .Select(o => new ClosedOrderSummaryDto(
                o.Id,
                o.TableNumber,
                o.Total.Amount,
                o.ClosedAt!.Value,
                o.Lines.Count))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<KitchenOrderLineDto>> BuildKitchenLinesAsync(
        Guid orderId,
        CancellationToken cancellationToken) =>
        await (
            from line in _context.OrderLines.AsNoTracking()
            join product in _context.Products.AsNoTracking() on line.ProductId equals product.Id
            where line.OrderId == orderId
            select new KitchenOrderLineDto(
                line.Id,
                product.Name,
                line.Quantity.Value,
                line.Notes,
                product.Category))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<OrderLineDto>> BuildOrderLinesAsync(
        Guid orderId,
        CancellationToken cancellationToken) =>
        await (
            from line in _context.OrderLines.AsNoTracking()
            join product in _context.Products.AsNoTracking() on line.ProductId equals product.Id
            where line.OrderId == orderId
            select new OrderLineDto(
                line.Id,
                line.ProductId,
                product.Name,
                product.Category,
                line.Quantity.Value,
                line.UnitPrice.Amount,
                line.Subtotal.Amount,
                line.Notes))
            .ToListAsync(cancellationToken);

    private static KitchenOrderDto? FilterKitchenOrderByCategory(KitchenOrderDto dto, string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return dto;

        var lines = dto.Lines
            .Where(l => string.Equals(l.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return lines.Count == 0
            ? null
            : dto with { Lines = lines };
    }
}
