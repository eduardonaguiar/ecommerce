namespace NotificationsWorker.Messaging;

public sealed record RabbitMqOptions
{
    public string HostName { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "ecommerce";
    public string Password { get; init; } = "ecommerce";
    public string QueueName { get; init; } = "notifications.send";
}
