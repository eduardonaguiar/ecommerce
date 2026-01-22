namespace Inventory.Models;

public sealed record StockItem
{
    public string ProductId { get; init; } = string.Empty;
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record StockReservation
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string Status { get; init; } = ReservationStatus.Pending;
    public string? FailureReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public static class ReservationStatus
{
    public const string Pending = "PENDING";
    public const string Reserved = "RESERVED";
    public const string Failed = "FAILED";
    public const string Committed = "COMMITTED";
    public const string Released = "RELEASED";
}

public sealed record InventorySettings
{
    public string DefaultProductId { get; init; } = "default";
    public int DefaultStock { get; init; } = 100;
    public int DefaultReservationQuantity { get; init; } = 1;
}

public sealed record InventoryItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static InventoryItemResponse FromItem(StockItem item) => new()
    {
        ProductId = item.ProductId,
        AvailableQuantity = item.AvailableQuantity,
        ReservedQuantity = item.ReservedQuantity,
        UpdatedAt = item.UpdatedAt
    };
}
