using System.Text.Json;
using Inventory.Data;
using Inventory.Models;
using Npgsql;

namespace Inventory.Messaging;

public sealed class InventorySagaHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly NpgsqlDataSource _dataSource;
    private readonly IInventoryRepository _repository;
    private readonly IInventoryEventPublisher _publisher;
    private readonly InventorySettings _settings;
    private readonly ILogger<InventorySagaHandler> _logger;

    public InventorySagaHandler(
        NpgsqlDataSource dataSource,
        IInventoryRepository repository,
        IInventoryEventPublisher publisher,
        InventorySettings settings,
        ILogger<InventorySagaHandler> logger)
    {
        _dataSource = dataSource;
        _repository = repository;
        _publisher = publisher;
        _settings = settings;
        _logger = logger;
    }

    public async Task HandleAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        switch (envelope.Type)
        {
            case OrderEventTypes.OrderCreated:
                await HandleOrderCreatedAsync(envelope, cancellationToken);
                break;
            case OrderEventTypes.OrderConfirmed:
                await HandleOrderConfirmedAsync(envelope, cancellationToken);
                break;
            case OrderEventTypes.OrderCancelled:
                await HandleOrderCancelledAsync(envelope, cancellationToken);
                break;
            default:
                return;
        }
    }

    private async Task HandleOrderCreatedAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        var data = Deserialize<OrderCreatedData>(envelope);
        if (data is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var existingReservation = await _repository.GetReservationByOrderIdForUpdateAsync(
            data.OrderId,
            connection,
            transaction,
            cancellationToken);

        if (existingReservation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            await PublishExistingReservationAsync(existingReservation, envelope, cancellationToken);
            return;
        }

        var stockItem = await _repository.GetStockItemForUpdateAsync(
            _settings.DefaultProductId,
            connection,
            transaction,
            cancellationToken);

        if (stockItem is null)
        {
            stockItem = new StockItem
            {
                ProductId = _settings.DefaultProductId,
                AvailableQuantity = _settings.DefaultStock,
                ReservedQuantity = 0,
                UpdatedAt = now
            };
            await _repository.UpsertStockItemAsync(stockItem, connection, transaction, cancellationToken);
        }

        if (stockItem.AvailableQuantity < _settings.DefaultReservationQuantity)
        {
            var failure = new StockReservation
            {
                Id = Guid.NewGuid(),
                OrderId = data.OrderId,
                ProductId = stockItem.ProductId,
                Quantity = _settings.DefaultReservationQuantity,
                Status = ReservationStatus.Failed,
                FailureReason = "insufficient_stock",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.CreateReservationAsync(failure, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _publisher.PublishStockFailedAsync(
                data.OrderId,
                failure.FailureReason ?? "insufficient_stock",
                envelope.RequestId,
                envelope.TraceId,
                envelope.SpanId,
                cancellationToken);

            _logger.LogInformation(
                "{event} stock reservation failed {order_id} {product_id}",
                "inventory.reservation.failed",
                data.OrderId,
                stockItem.ProductId);
            return;
        }

        var updatedStock = stockItem with
        {
            AvailableQuantity = stockItem.AvailableQuantity - _settings.DefaultReservationQuantity,
            ReservedQuantity = stockItem.ReservedQuantity + _settings.DefaultReservationQuantity,
            UpdatedAt = now
        };

        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            OrderId = data.OrderId,
            ProductId = stockItem.ProductId,
            Quantity = _settings.DefaultReservationQuantity,
            Status = ReservationStatus.Reserved,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.UpsertStockItemAsync(updatedStock, connection, transaction, cancellationToken);
        await _repository.CreateReservationAsync(reservation, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _publisher.PublishStockReservedAsync(
            data.OrderId,
            reservation.Id.ToString("N"),
            envelope.RequestId,
            envelope.TraceId,
            envelope.SpanId,
            cancellationToken);

        _logger.LogInformation(
            "{event} stock reserved {order_id} {reservation_id} {product_id}",
            "inventory.reservation.reserved",
            data.OrderId,
            reservation.Id,
            stockItem.ProductId);
    }

    private async Task HandleOrderConfirmedAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        var data = Deserialize<OrderConfirmedData>(envelope);
        if (data is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var reservation = await _repository.GetReservationByOrderIdForUpdateAsync(
            data.OrderId,
            connection,
            transaction,
            cancellationToken);

        if (reservation is null)
        {
            _logger.LogWarning(
                "{event} order confirmed without reservation {order_id}",
                "inventory.commit.missing",
                data.OrderId);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (reservation.Status != ReservationStatus.Reserved)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var stockItem = await _repository.GetStockItemForUpdateAsync(
            reservation.ProductId,
            connection,
            transaction,
            cancellationToken);

        if (stockItem is null)
        {
            _logger.LogWarning(
                "{event} stock item missing while committing {order_id} {product_id}",
                "inventory.commit.missing_stock",
                data.OrderId,
                reservation.ProductId);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var updatedStock = stockItem with
        {
            ReservedQuantity = Math.Max(0, stockItem.ReservedQuantity - reservation.Quantity),
            UpdatedAt = now
        };

        var updatedReservation = reservation with
        {
            Status = ReservationStatus.Committed,
            UpdatedAt = now
        };

        await _repository.UpsertStockItemAsync(updatedStock, connection, transaction, cancellationToken);
        await _repository.UpdateReservationAsync(updatedReservation, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "{event} stock committed {order_id} {reservation_id}",
            "inventory.reservation.committed",
            data.OrderId,
            reservation.Id);
    }

    private async Task HandleOrderCancelledAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        var data = Deserialize<OrderCancelledData>(envelope);
        if (data is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var reservation = await _repository.GetReservationByOrderIdForUpdateAsync(
            data.OrderId,
            connection,
            transaction,
            cancellationToken);

        if (reservation is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (reservation.Status != ReservationStatus.Reserved)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var stockItem = await _repository.GetStockItemForUpdateAsync(
            reservation.ProductId,
            connection,
            transaction,
            cancellationToken);

        if (stockItem is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var updatedStock = stockItem with
        {
            AvailableQuantity = stockItem.AvailableQuantity + reservation.Quantity,
            ReservedQuantity = Math.Max(0, stockItem.ReservedQuantity - reservation.Quantity),
            UpdatedAt = now
        };

        var updatedReservation = reservation with
        {
            Status = ReservationStatus.Released,
            UpdatedAt = now
        };

        await _repository.UpsertStockItemAsync(updatedStock, connection, transaction, cancellationToken);
        await _repository.UpdateReservationAsync(updatedReservation, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "{event} stock reservation released {order_id} {reservation_id}",
            "inventory.reservation.released",
            data.OrderId,
            reservation.Id);
    }

    private async Task PublishExistingReservationAsync(
        StockReservation reservation,
        EventEnvelope<JsonElement> envelope,
        CancellationToken cancellationToken)
    {
        if (reservation.Status == ReservationStatus.Failed)
        {
            await _publisher.PublishStockFailedAsync(
                reservation.OrderId,
                reservation.FailureReason ?? "reservation_failed",
                envelope.RequestId,
                envelope.TraceId,
                envelope.SpanId,
                cancellationToken);
            return;
        }

        await _publisher.PublishStockReservedAsync(
            reservation.OrderId,
            reservation.Id.ToString("N"),
            envelope.RequestId,
            envelope.TraceId,
            envelope.SpanId,
            cancellationToken);
    }

    private T? Deserialize<T>(EventEnvelope<JsonElement> envelope)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(envelope.Data.GetRawText(), SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "{event} invalid event payload", "inventory.saga.invalid_payload");
            return default;
        }
    }
}
