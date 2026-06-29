using Microsoft.EntityFrameworkCore;
using Resto.Application.Common.Helpers;
using Resto.Application.Common.Interfaces;
using Resto.Domain.CashRegister;
using Resto.Domain.Orders;
using Resto.Domain.Payments;
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
        var orderHeaders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.ConfirmadoEnCocina && o.SentToKitchenAt != null)
            .OrderBy(o => o.SentToKitchenAt)
            .Select(o => new { o.Id, o.TableNumber, SentToKitchenAt = o.SentToKitchenAt!.Value })
            .ToListAsync(cancellationToken);

        if (orderHeaders.Count == 0)
            return [];

        var orderIds = orderHeaders.Select(o => o.Id).ToList();

        var lineRows = await (
            from line in _context.OrderLines.AsNoTracking()
            join product in _context.Products.AsNoTracking() on line.ProductId equals product.Id
            where orderIds.Contains(line.OrderId)
            select new
            {
                line.OrderId,
                Line = new KitchenOrderLineDto(
                    line.Id,
                    product.Name,
                    line.Quantity.Value,
                    line.Notes,
                    product.Category),
            })
            .ToListAsync(cancellationToken);

        var linesByOrder = lineRows
            .GroupBy(x => x.OrderId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<KitchenOrderLineDto>)g.Select(x => x.Line).ToList());

        var result = new List<KitchenOrderDto>(orderHeaders.Count);

        foreach (var header in orderHeaders)
        {
            if (!linesByOrder.TryGetValue(header.Id, out var lines))
                lines = [];

            var dto = new KitchenOrderDto(header.Id, header.TableNumber, header.SentToKitchenAt, lines);
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
        var paymentMethod = await GetPaymentMethodForOrderAsync(orderId, cancellationToken);

        return new OrderDetailDto(
            order.Id,
            order.TableNumber,
            order.Status.ToString(),
            order.Total.Amount,
            RowVersionHelper.ToBase64(order.RowVersion),
            order.CreatedAt,
            order.SentToKitchenAt,
            order.ClosedAt,
            paymentMethod,
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
        return await (
            from o in _context.Orders.AsNoTracking()
            join p in _context.Payments.AsNoTracking() on o.Id equals p.OrderId into payments
            from payment in payments.DefaultIfEmpty()
            where o.Status == OrderStatus.Cerrado
                && o.ClosedAt >= rangeStartUtc
                && o.ClosedAt < rangeEndUtc
            orderby o.ClosedAt descending
            select new ClosedOrderSummaryDto(
                o.Id,
                o.TableNumber,
                o.Total.Amount,
                o.ClosedAt!.Value,
                o.Lines.Count,
                payment != null ? payment.Method.ToString() : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<CashShiftDetailDto?> GetCurrentCashShiftAsync(
        CancellationToken cancellationToken = default)
    {
        var shift = await _context.CashRegisterShifts
            .AsNoTracking()
            .Where(s => s.Status == CashShiftStatus.Open)
            .Select(s => new
            {
                s.Id,
                s.OpenedAt,
                s.OpenedByUserId,
                OpeningFloat = s.OpeningFloat.Amount,
                s.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (shift is null)
            return null;

        var summary = await BuildShiftSummaryAsync(shift.Id, shift.OpeningFloat, cancellationToken);

        return new CashShiftDetailDto(
            shift.Id,
            shift.OpenedAt,
            shift.OpenedByUserId,
            shift.OpeningFloat,
            shift.Status.ToString(),
            summary);
    }

    private async Task<string?> GetPaymentMethodForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken) =>
        await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .Select(p => p.Method.ToString())
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<CashShiftSummaryDto> BuildShiftSummaryAsync(
        Guid shiftId,
        decimal openingFloat,
        CancellationToken cancellationToken)
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CashRegisterShiftId == shiftId)
            .Select(p => new { p.Method, Amount = p.Amount.Amount })
            .ToListAsync(cancellationToken);

        var totalCash = payments
            .Where(p => p.Method == PaymentMethod.Cash)
            .Sum(p => p.Amount);

        var totalCard = payments
            .Where(p => p.Method == PaymentMethod.Card)
            .Sum(p => p.Amount);

        var totalTransfer = payments
            .Where(p => p.Method == PaymentMethod.Transfer)
            .Sum(p => p.Amount);

        return new CashShiftSummaryDto(
            payments.Count,
            totalCash,
            totalCard,
            totalTransfer,
            totalCash + totalCard + totalTransfer,
            openingFloat + totalCash);
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
