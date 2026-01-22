using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Orders.Models;

namespace Orders.Messaging;

public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(Order order, string requestId, CancellationToken cancellationToken);
    Task PublishOrderConfirmedAsync(Order order, string requestId, string? traceId, string? spanId, CancellationToken cancellationToken);
    Task PublishOrderCancelledAsync(Order order, string requestId, string? traceId, string? spanId, string reason, CancellationToken cancellationToken);
}

public sealed class KafkaOrderEventPublisher : IOrderEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly string _source;

    public KafkaOrderEventPublisher(IProducer<string, string> producer, string topic, string source)
    {
        _producer = producer;
        _topic = topic;
        _source = source;
    }

    public Task PublishOrderCreatedAsync(Order order, string requestId, CancellationToken cancellationToken)
    {
        var envelope = BuildEnvelope(
            OrderEventTypes.OrderCreated,
            requestId,
            new OrderCreatedData
            {
                OrderId = order.Id,
                Amount = order.Amount,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            });

        return PublishAsync(order.Id.ToString(), envelope, cancellationToken);
    }

    public Task PublishOrderConfirmedAsync(Order order, string requestId, string? traceId, string? spanId, CancellationToken cancellationToken)
    {
        var envelope = BuildEnvelope(
            OrderEventTypes.OrderConfirmed,
            requestId,
            new OrderConfirmedData
            {
                OrderId = order.Id,
                Status = order.Status,
                ConfirmedAt = order.UpdatedAt
            },
            traceId,
            spanId);

        return PublishAsync(order.Id.ToString(), envelope, cancellationToken);
    }

    public Task PublishOrderCancelledAsync(Order order, string requestId, string? traceId, string? spanId, string reason, CancellationToken cancellationToken)
    {
        var envelope = BuildEnvelope(
            OrderEventTypes.OrderCancelled,
            requestId,
            new OrderCancelledData
            {
                OrderId = order.Id,
                Status = order.Status,
                Reason = reason,
                CancelledAt = order.UpdatedAt
            },
            traceId,
            spanId);

        return PublishAsync(order.Id.ToString(), envelope, cancellationToken);
    }

    private EventEnvelope<T> BuildEnvelope<T>(
        string type,
        string requestId,
        T data,
        string? traceId = null,
        string? spanId = null)
    {
        var activity = Activity.Current;
        return new EventEnvelope<T>
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Source = _source,
            Time = DateTime.UtcNow,
            TraceId = traceId ?? activity?.TraceId.ToString(),
            SpanId = spanId ?? activity?.SpanId.ToString(),
            RequestId = requestId,
            Version = "1",
            Data = data
        };
    }

    private async Task PublishAsync<T>(string key, EventEnvelope<T> envelope, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(envelope, SerializerOptions);
        await _producer.ProduceAsync(
            _topic,
            new Message<string, string> { Key = key, Value = payload },
            cancellationToken);
    }
}
