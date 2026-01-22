using System.Diagnostics;
using System.Text.Json;
using Catalog.Models;
using Confluent.Kafka;

namespace Catalog.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly string _source;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public KafkaEventPublisher(IProducer<string, string> producer, string topic, string source)
    {
        _producer = producer;
        _topic = topic;
        _source = source;
    }

    public async Task PublishProductUpsertedAsync(Product product, string requestId, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        var envelope = new EventEnvelope<ProductUpsertedEventData>
        {
            Id = Guid.NewGuid().ToString(),
            Type = "catalog.product.upserted",
            Source = _source,
            Time = DateTime.UtcNow.ToString("O"),
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString(),
            RequestId = requestId,
            Version = "1",
            Data = new ProductUpsertedEventData
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                Currency = product.Currency,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                UpdatedAt = product.UpdatedAt
            }
        };

        var payload = JsonSerializer.Serialize(envelope, SerializerOptions);
        var message = new Message<string, string>
        {
            Key = product.Id,
            Value = payload
        };

        await _producer.ProduceAsync(_topic, message, cancellationToken).ConfigureAwait(false);
    }
}
