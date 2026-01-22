using System.Text.Json;
using Confluent.Kafka;
using Serilog.Context;

namespace Inventory.Messaging;

public sealed class InventorySagaConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<InventorySagaConsumer> _logger;
    private readonly InventorySagaHandler _handler;
    private readonly string _topic;

    public InventorySagaConsumer(
        IConsumer<string, string> consumer,
        InventorySagaHandler handler,
        ILogger<InventorySagaConsumer> logger,
        IConfiguration configuration)
    {
        _consumer = consumer;
        _handler = handler;
        _logger = logger;
        _topic = configuration["Kafka:OrdersTopic"]
            ?? configuration["KAFKA_ORDERS_TOPIC"]
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
                    _logger.LogWarning("{event} unable to deserialize event payload", "inventory.event.deserialize_failed");
                    _consumer.Commit(result);
                    continue;
                }

                using (LogContext.PushProperty("request_id", envelope.RequestId))
                {
                    await _handler.HandleAsync(envelope, stoppingToken);
                }

                _consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "{event} kafka consume failure", "inventory.kafka.consume_failed");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{event} inventory saga handler failure", "inventory.saga.failed");
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
