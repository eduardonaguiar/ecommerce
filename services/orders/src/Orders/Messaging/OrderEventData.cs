namespace Orders.Messaging;

public sealed record OrderCreatedData
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record OrderConfirmedData
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ConfirmedAt { get; init; }
}

public sealed record OrderCancelledData
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}

public sealed record StockReservedData
{
    public Guid OrderId { get; init; }
    public string? ReservationId { get; init; }
}

public sealed record StockFailedData
{
    public Guid OrderId { get; init; }
    public string? Reason { get; init; }
}

public sealed record PaymentProcessedData
{
    public Guid OrderId { get; init; }
    public string? PaymentId { get; init; }
}
