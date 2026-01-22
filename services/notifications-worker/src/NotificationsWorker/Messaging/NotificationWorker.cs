using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace NotificationsWorker.Messaging;

public sealed class NotificationWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnection _connection;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly NotificationWorkerOptions _workerOptions;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly ActivitySource _activitySource;
    private IModel? _channel;

    public NotificationWorker(
        IConnection connection,
        RabbitMqOptions rabbitOptions,
        NotificationWorkerOptions workerOptions,
        ILogger<NotificationWorker> logger,
        ActivitySource activitySource)
    {
        _connection = connection;
        _rabbitOptions = rabbitOptions;
        _workerOptions = workerOptions;
        _logger = logger;
        _activitySource = activitySource;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: _rabbitOptions.QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.BasicQos(0, _workerOptions.PrefetchCount, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) => await HandleMessageAsync(args, stoppingToken);
        _channel.BasicConsume(queue: _rabbitOptions.QueueName, autoAck: false, consumer: consumer);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore shutdown.
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            return;
        }

        var payload = Encoding.UTF8.GetString(args.Body.ToArray());
        var job = Deserialize(payload);
        if (job is null)
        {
            _logger.LogWarning("{event} invalid notification job payload", "notifications.job.invalid");
            _channel.BasicAck(args.DeliveryTag, false);
            return;
        }

        var requestId = string.IsNullOrWhiteSpace(job.RequestId) ? "unknown" : job.RequestId;

        using (LogContext.PushProperty("request_id", requestId))
        using (var activity = StartActivity(job))
        {
            var attempts = 0;
            var delay = TimeSpan.FromSeconds(Math.Max(1, _workerOptions.BaseDelaySeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                attempts++;
                try
                {
                    await SimulateSendAsync(job, stoppingToken);
                    _logger.LogInformation(
                        "{event} notification delivered {order_id} {event_type}",
                        "notifications.sent",
                        job.OrderId,
                        job.EventType);
                    _channel.BasicAck(args.DeliveryTag, false);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "{event} notification delivery failed attempt {attempt}",
                        "notifications.send_failed",
                        attempts);

                    if (attempts >= _workerOptions.MaxRetries)
                    {
                        _logger.LogError(
                            "{event} notification delivery abandoned after retries {order_id}",
                            "notifications.send_abandoned",
                            job.OrderId);
                        _channel.BasicAck(args.DeliveryTag, false);
                        return;
                    }

                    await Task.Delay(delay, stoppingToken);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                }
            }
        }
    }

    private Activity? StartActivity(NotificationJob job)
    {
        ActivityContext parentContext = default;
        if (!string.IsNullOrWhiteSpace(job.TraceId) && !string.IsNullOrWhiteSpace(job.SpanId))
        {
            ActivityContext.TryParse(job.TraceId, job.SpanId, ActivityTraceFlags.Recorded, out parentContext);
        }

        var activity = parentContext == default
            ? _activitySource.StartActivity("notifications.send", ActivityKind.Consumer)
            : _activitySource.StartActivity("notifications.send", ActivityKind.Consumer, parentContext);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", _rabbitOptions.QueueName);
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("event.type", job.EventType);
        activity?.SetTag("order.id", job.OrderId.ToString());
        activity?.SetTag("request.id", job.RequestId);

        return activity;
    }

    private async Task SimulateSendAsync(NotificationJob job, CancellationToken stoppingToken)
    {
        if (_workerOptions.SimulatedFailureRate > 0 && Random.Shared.NextDouble() < _workerOptions.SimulatedFailureRate)
        {
            throw new InvalidOperationException("Simulated notification delivery failure.");
        }

        _logger.LogInformation(
            "{event} sending notification {order_id} {event_type} {status}",
            "notifications.send.start",
            job.OrderId,
            job.EventType,
            job.Status ?? string.Empty);

        await Task.Delay(TimeSpan.FromMilliseconds(150), stoppingToken);
    }

    private static NotificationJob? Deserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<NotificationJob>(payload, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}
