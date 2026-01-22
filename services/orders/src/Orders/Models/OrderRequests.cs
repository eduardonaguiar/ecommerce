namespace Orders.Models;

public sealed record OrderCreateRequest
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? CustomerId { get; init; }
}

public sealed record OrderResponse
{
    public Guid Id { get; init; }
    public OrderStatus Status { get; init; }
    public StockStatus StockStatus { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static OrderResponse FromOrder(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            Status = order.Status,
            StockStatus = order.StockStatus,
            PaymentStatus = order.PaymentStatus,
            Amount = order.Amount,
            Currency = order.Currency,
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
