namespace NotificationsWorker.Messaging;

public sealed record NotificationWorkerOptions
{
    public int MaxRetries { get; init; } = 3;
    public int BaseDelaySeconds { get; init; } = 2;
    public double SimulatedFailureRate { get; init; }
    public ushort PrefetchCount { get; init; } = 4;
}
