using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;

namespace Inventory.Messaging;

public interface IInventoryEventPublisher
{
    Task PublishStockReservedAsync(Guid orderId, string reservationId, string requestId, string? traceId, string? spanId, CancellationToken cancellationToken);
    Task PublishStockFailedAsync(Guid orderId, string reason, string requestId, string? traceId, string? spanId, CancellationToken cancellationToken);
}

public sealed class KafkaInventoryEventPublisher : IInventoryEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly string _source;

    public KafkaInventoryEventPublisher(IProducer<string, string> producer, string topic, string source)
    {
        _producer = producer;
        _topic = topic;
        _source = source;
    }

    public Task PublishStockReservedAsync(
        Guid orderId,
        string reservationId,
        string requestId,
        string? traceId,
        string? spanId,
        CancellationToken cancellationToken)
    {
        var envelope = BuildEnvelope(
            InventoryEventTypes.StockReserved,
            requestId,
            new StockReservedData
            {
                OrderId = orderId,
                ReservationId = reservationId
            },
            traceId,
            spanId);

        return PublishAsync(orderId.ToString(), envelope, cancellationToken);
    }

    public Task PublishStockFailedAsync(
        Guid orderId,
        string reason,
        string requestId,
        string? traceId,
        string? spanId,
        CancellationToken cancellationToken)
    {
        var envelope = BuildEnvelope(
            InventoryEventTypes.StockFailed,
            requestId,
            new StockFailedData
            {
                OrderId = orderId,
                Reason = reason
            },
            traceId,
            spanId);

        return PublishAsync(orderId.ToString(), envelope, cancellationToken);
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
