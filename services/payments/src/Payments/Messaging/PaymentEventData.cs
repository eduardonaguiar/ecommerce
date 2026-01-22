namespace Payments.Messaging;

public sealed record PaymentProcessedData
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime ProcessedAt { get; init; }
}
