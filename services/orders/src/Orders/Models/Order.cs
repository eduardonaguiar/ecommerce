namespace Orders.Models;

public sealed record Order
{
    public Guid Id { get; init; }
    public OrderStatus Status { get; init; } = OrderStatus.Pending;
    public StockStatus StockStatus { get; init; } = StockStatus.Pending;
    public PaymentStatus PaymentStatus { get; init; } = PaymentStatus.Pending;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? CustomerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
