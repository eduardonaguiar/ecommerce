using Inventory.Models;
using Npgsql;

namespace Inventory.Data;

public interface IInventoryRepository
{
    Task<StockItem?> GetStockItemAsync(string productId, CancellationToken cancellationToken);
    Task<StockItem?> GetStockItemForUpdateAsync(string productId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task UpsertStockItemAsync(StockItem item, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task<StockReservation?> GetReservationByOrderIdForUpdateAsync(Guid orderId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task CreateReservationAsync(StockReservation reservation, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task UpdateReservationAsync(StockReservation reservation, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
}

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public InventoryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<StockItem?> GetStockItemAsync(string productId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT product_id, available_quantity, reserved_quantity, updated_at
            FROM stock_items
            WHERE product_id = @product_id;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("product_id", productId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapStockItem(reader) : null;
    }

    public async Task<StockItem?> GetStockItemForUpdateAsync(
        string productId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT product_id, available_quantity, reserved_quantity, updated_at
            FROM stock_items
            WHERE product_id = @product_id
            FOR UPDATE;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("product_id", productId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapStockItem(reader) : null;
    }

    public async Task UpsertStockItemAsync(
        StockItem item,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO stock_items (product_id, available_quantity, reserved_quantity, updated_at)
            VALUES (@product_id, @available_quantity, @reserved_quantity, @updated_at)
            ON CONFLICT (product_id)
            DO UPDATE SET
                available_quantity = EXCLUDED.available_quantity,
                reserved_quantity = EXCLUDED.reserved_quantity,
                updated_at = EXCLUDED.updated_at;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("product_id", item.ProductId);
        command.Parameters.AddWithValue("available_quantity", item.AvailableQuantity);
        command.Parameters.AddWithValue("reserved_quantity", item.ReservedQuantity);
        command.Parameters.AddWithValue("updated_at", item.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<StockReservation?> GetReservationByOrderIdForUpdateAsync(
        Guid orderId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, order_id, product_id, quantity, status, failure_reason, created_at, updated_at
            FROM stock_reservations
            WHERE order_id = @order_id
            FOR UPDATE;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("order_id", orderId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReservation(reader) : null;
    }

    public async Task CreateReservationAsync(
        StockReservation reservation,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO stock_reservations
                (id, order_id, product_id, quantity, status, failure_reason, created_at, updated_at)
            VALUES
                (@id, @order_id, @product_id, @quantity, @status, @failure_reason, @created_at, @updated_at);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", reservation.Id);
        command.Parameters.AddWithValue("order_id", reservation.OrderId);
        command.Parameters.AddWithValue("product_id", reservation.ProductId);
        command.Parameters.AddWithValue("quantity", reservation.Quantity);
        command.Parameters.AddWithValue("status", reservation.Status);
        command.Parameters.AddWithValue("failure_reason", (object?)reservation.FailureReason ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", reservation.CreatedAt);
        command.Parameters.AddWithValue("updated_at", reservation.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateReservationAsync(
        StockReservation reservation,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE stock_reservations
            SET status = @status,
                failure_reason = @failure_reason,
                updated_at = @updated_at
            WHERE id = @id;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", reservation.Id);
        command.Parameters.AddWithValue("status", reservation.Status);
        command.Parameters.AddWithValue("failure_reason", (object?)reservation.FailureReason ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_at", reservation.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static StockItem MapStockItem(NpgsqlDataReader reader) => new()
    {
        ProductId = reader.GetString(0),
        AvailableQuantity = reader.GetInt32(1),
        ReservedQuantity = reader.GetInt32(2),
        UpdatedAt = reader.GetDateTime(3)
    };

    private static StockReservation MapReservation(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        OrderId = reader.GetGuid(1),
        ProductId = reader.GetString(2),
        Quantity = reader.GetInt32(3),
        Status = reader.GetString(4),
        FailureReason = reader.IsDBNull(5) ? null : reader.GetString(5),
        CreatedAt = reader.GetDateTime(6),
        UpdatedAt = reader.GetDateTime(7)
    };
}
