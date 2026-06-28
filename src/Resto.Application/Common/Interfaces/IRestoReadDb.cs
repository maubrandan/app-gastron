namespace Resto.Application.Common.Interfaces;

public interface IRestoReadDb
{
    Task<IReadOnlyList<KitchenOrderDto>> GetActiveKitchenOrdersAsync(
        string? category = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TableDto>> GetTablesAsync(CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetActiveOrderByTableAsync(int tableNumber, CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<KitchenOrderDto?> GetKitchenOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<DailySummaryDto> GetDailySummaryAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        string timeZoneId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClosedOrderSummaryDto>> GetClosedOrdersAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken = default);
}

public sealed record KitchenOrderLineDto(Guid Id, string ProductName, int Quantity, string? Notes, string Category);

public sealed record KitchenOrderDto(
    Guid Id,
    int TableNumber,
    DateTime SentToKitchenAt,
    IReadOnlyList<KitchenOrderLineDto> Lines);

public sealed record TableDto(
    int Number,
    string Status,
    string RowVersion,
    Guid? ActiveOrderId);

public sealed record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Category,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    string? Notes);

public sealed record OrderDetailDto(
    Guid Id,
    int TableNumber,
    string Status,
    decimal Total,
    string RowVersion,
    DateTime CreatedAt,
    DateTime? SentToKitchenAt,
    DateTime? ClosedAt,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Category,
    bool IsActive);

public sealed record DailySummaryDto(
    int OrderCount,
    decimal TotalRevenue,
    decimal AverageTicket,
    IReadOnlyList<OrdersByHourDto> OrdersByHour);

public sealed record OrdersByHourDto(int Hour, int OrderCount, decimal Revenue);

public sealed record ClosedOrderSummaryDto(
    Guid Id,
    int TableNumber,
    decimal Total,
    DateTime ClosedAt,
    int LineCount);
