using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Payments.Models;

namespace Payments.Messaging;

public interface IPaymentEventPublisher
{
    Task PublishPaymentProcessedAsync(PaymentAttempt attempt, string requestId, CancellationToken cancellationToken);
}

public sealed class KafkaPaymentEventPublisher : IPaymentEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly string _source;

    public KafkaPaymentEventPublisher(IProducer<string, string> producer, string topic, string source)
    {
        _producer = producer;
        _topic = topic;
        _source = source;
    }

    public Task PublishPaymentProcessedAsync(PaymentAttempt attempt, string requestId, CancellationToken cancellationToken)
    {
        var status = attempt.Status == PaymentStatus.Success ? "success" : "failure";
        var envelope = BuildEnvelope(
            PaymentEventTypes.PaymentProcessed,
            requestId,
            new PaymentProcessedData
            {
                OrderId = attempt.OrderId,
                PaymentId = attempt.Id,
                Status = status,
                Reason = attempt.FailureReason,
                Amount = attempt.Amount,
                Currency = attempt.Currency,
                ProcessedAt = attempt.CreatedAt
            });

        return PublishAsync(attempt.OrderId.ToString(), envelope, cancellationToken);
    }

    private EventEnvelope<T> BuildEnvelope<T>(string type, string requestId, T data)
    {
        var activity = Activity.Current;
        return new EventEnvelope<T>
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Source = _source,
            Time = DateTime.UtcNow,
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString(),
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
