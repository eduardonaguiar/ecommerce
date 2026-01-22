using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using Orders.Data;
using Orders.Models;

namespace Orders.Messaging;

public sealed class OrderSagaHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    private readonly NpgsqlDataSource _dataSource;
    private readonly IOrderRepository _repository;
    private readonly IOrderEventPublisher _publisher;
    private readonly ILogger<OrderSagaHandler> _logger;

    public OrderSagaHandler(
        NpgsqlDataSource dataSource,
        IOrderRepository repository,
        IOrderEventPublisher publisher,
        ILogger<OrderSagaHandler> logger)
    {
        _dataSource = dataSource;
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task HandleAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        switch (envelope.Type)
        {
            case OrderEventTypes.StockReserved:
                await HandleStockReservedAsync(envelope, cancellationToken);
                break;
            case OrderEventTypes.StockFailed:
                await HandleStockFailedAsync(envelope, cancellationToken);
                break;
            case OrderEventTypes.PaymentProcessed:
                await HandlePaymentProcessedAsync(envelope, cancellationToken);
                break;
            default:
                return;
        }
    }

    private async Task HandleStockReservedAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        var data = Deserialize<StockReservedData>(envelope);
        if (data is null)
        {
            return;
        }

        await ApplyTransitionAsync(
            data.OrderId,
            envelope,
            order => OrderStateMachine.ApplyStockReserved(order),
            cancellationToken);
    }

    private async Task HandleStockFailedAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        var data = Deserialize<StockFailedData>(envelope);
        if (data is null)
        {
            return;
        }

        await ApplyTransitionAsync(
            data.OrderId,
            envelope,
            order => OrderStateMachine.ApplyStockFailed(order, data.Reason),
            cancellationToken);
    }

    private async Task HandlePaymentProcessedAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        var data = Deserialize<PaymentProcessedData>(envelope);
        if (data is null)
        {
            return;
        }

        await ApplyTransitionAsync(
            data.OrderId,
            envelope,
            order => OrderStateMachine.ApplyPaymentProcessed(order),
            cancellationToken);
    }

    private async Task ApplyTransitionAsync(
        Guid orderId,
        EventEnvelope<JsonElement> envelope,
        Func<Order, OrderTransition> transitionFunc,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var order = await _repository.GetByIdForUpdateAsync(orderId, connection, transaction, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning(
                "{event} order not found for saga update {order_id} {event_type}",
                "orders.saga.missing",
                orderId,
                envelope.Type);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var transition = transitionFunc(order);
        if (!transition.HasChange)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var updatedOrder = order with
        {
            Status = transition.Status,
            StockStatus = transition.StockStatus,
            PaymentStatus = transition.PaymentStatus,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpdateAsync(updatedOrder, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "{event} applied order transition {order_id} {trigger} {status} {stock_status} {payment_status}",
            "orders.saga.transition",
            orderId,
            transition.Trigger,
            updatedOrder.Status,
            updatedOrder.StockStatus,
            updatedOrder.PaymentStatus);

        if (transition.PublishConfirmed)
        {
            await _publisher.PublishOrderConfirmedAsync(updatedOrder, envelope.RequestId, envelope.TraceId, envelope.SpanId, cancellationToken);
        }

        if (transition.PublishCancelled)
        {
            var reason = transition.CancelReason ?? transition.Trigger;
            await _publisher.PublishOrderCancelledAsync(updatedOrder, envelope.RequestId, envelope.TraceId, envelope.SpanId, reason, cancellationToken);
        }
    }

    private T? Deserialize<T>(EventEnvelope<JsonElement> envelope)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(envelope.Data.GetRawText(), SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "{event} invalid event payload", "orders.saga.invalid_payload");
            return default;
        }
    }
}
