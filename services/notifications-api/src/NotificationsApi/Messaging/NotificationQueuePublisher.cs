using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace NotificationsApi.Messaging;

public sealed class NotificationQueuePublisher : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly ILogger<NotificationQueuePublisher> _logger;

    public NotificationQueuePublisher(
        IConnection connection,
        RabbitMqOptions options,
        ILogger<NotificationQueuePublisher> logger)
    {
        _logger = logger;
        _queueName = options.QueueName;
        _channel = connection.CreateModel();
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
    }

    public Task PublishAsync(NotificationJob job, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(job, SerializerOptions);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.CorrelationId = job.RequestId;
        properties.Headers = new Dictionary<string, object?>
        {
            ["trace_id"] = job.TraceId ?? string.Empty,
            ["span_id"] = job.SpanId ?? string.Empty,
            ["request_id"] = job.RequestId,
            ["event_type"] = job.EventType
        };

        _channel.BasicPublish(exchange: string.Empty, routingKey: _queueName, basicProperties: properties, body: body);

        _logger.LogInformation(
            "{event} enqueued notification job {order_id} {event_type}",
            "notifications.job.enqueued",
            job.OrderId,
            job.EventType);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Close();
        _channel.Dispose();
    }
}
