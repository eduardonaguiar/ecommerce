namespace NotificationsWorker.Messaging;

public sealed record NotificationJob
{
    public string EventType { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public string? Status { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; }
    public string RequestId { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
