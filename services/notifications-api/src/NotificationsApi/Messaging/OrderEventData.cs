namespace NotificationsApi.Messaging;

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
