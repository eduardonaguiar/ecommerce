using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Serilog.Context;

namespace NotificationsApi.Messaging;

public sealed class NotificationEventConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConsumer<string, string> _consumer;
    private readonly NotificationQueuePublisher _publisher;
    private readonly ILogger<NotificationEventConsumer> _logger;
    private readonly ActivitySource _activitySource;
    private readonly string _topic;

    public NotificationEventConsumer(
        IConsumer<string, string> consumer,
        NotificationQueuePublisher publisher,
        ILogger<NotificationEventConsumer> logger,
        ActivitySource activitySource,
        IConfiguration configuration)
    {
        _consumer = consumer;
        _publisher = publisher;
        _logger = logger;
        _activitySource = activitySource;
        _topic = configuration["Kafka:Topic"]
            ?? configuration["KAFKA_TOPIC"]
            ?? "orders.events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                var envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(result.Message.Value, SerializerOptions);
                if (envelope is null)
                {
                    _logger.LogWarning("{event} unable to deserialize order event", "notifications.event.deserialize_failed");
                    _consumer.Commit(result);
                    continue;
                }

                var job = CreateJob(envelope);
                if (job is null)
                {
                    _consumer.Commit(result);
                    continue;
                }

                using (LogContext.PushProperty("request_id", job.RequestId))
                using (var activity = StartActivity(envelope, job))
                {
                    try
                    {
                        await _publisher.PublishAsync(job, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "{event} failed to enqueue notification job {order_id}",
                            "notifications.job.enqueue_failed",
                            job.OrderId);
                    }
                }

                _consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "{event} kafka consume failure", "notifications.kafka.consume_failed");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{event} notification handler failure", "notifications.handler.failed");
            }
        }
    }

    private Activity? StartActivity(EventEnvelope<JsonElement> envelope, NotificationJob job)
    {
        ActivityContext parentContext = default;
        if (!string.IsNullOrWhiteSpace(envelope.TraceId) && !string.IsNullOrWhiteSpace(envelope.SpanId))
        {
            ActivityContext.TryParse(envelope.TraceId, envelope.SpanId, ActivityTraceFlags.Recorded, out parentContext);
        }

        var activity = parentContext == default
            ? _activitySource.StartActivity("notifications.consume", ActivityKind.Consumer)
            : _activitySource.StartActivity("notifications.consume", ActivityKind.Consumer, parentContext);

        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination", _topic);
        activity?.SetTag("messaging.operation", "receive");
        activity?.SetTag("event.type", job.EventType);
        activity?.SetTag("order.id", job.OrderId.ToString());
        activity?.SetTag("request.id", job.RequestId);

        return activity;
    }

    private NotificationJob? CreateJob(EventEnvelope<JsonElement> envelope)
    {
        var requestId = string.IsNullOrWhiteSpace(envelope.RequestId) ? "unknown" : envelope.RequestId;

        return envelope.Type switch
        {
            OrderEventTypes.OrderConfirmed => BuildConfirmedJob(envelope, requestId),
            OrderEventTypes.OrderCancelled => BuildCancelledJob(envelope, requestId),
            _ => null
        };
    }

    private NotificationJob? BuildConfirmedJob(EventEnvelope<JsonElement> envelope, string requestId)
    {
        var data = Deserialize<OrderConfirmedData>(envelope);
        if (data is null)
        {
            _logger.LogWarning("{event} missing order confirmed payload", "notifications.event.invalid");
            return null;
        }

        return new NotificationJob
        {
            EventType = envelope.Type,
            OrderId = data.OrderId,
            Status = data.Status,
            OccurredAt = data.ConfirmedAt,
            RequestId = requestId,
            TraceId = envelope.TraceId,
            SpanId = envelope.SpanId
        };
    }

    private NotificationJob? BuildCancelledJob(EventEnvelope<JsonElement> envelope, string requestId)
    {
        var data = Deserialize<OrderCancelledData>(envelope);
        if (data is null)
        {
            _logger.LogWarning("{event} missing order cancelled payload", "notifications.event.invalid");
            return null;
        }

        return new NotificationJob
        {
            EventType = envelope.Type,
            OrderId = data.OrderId,
            Status = data.Status,
            Reason = data.Reason,
            OccurredAt = data.CancelledAt,
            RequestId = requestId,
            TraceId = envelope.TraceId,
            SpanId = envelope.SpanId
        };
    }

    private static T? Deserialize<T>(EventEnvelope<JsonElement> envelope)
    {
        try
        {
            return envelope.Data.Deserialize<T>(SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
