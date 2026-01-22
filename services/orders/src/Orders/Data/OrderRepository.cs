using Npgsql;
using Orders.Models;

namespace Orders.Data;

public interface IOrderRepository
{
    Task CreateAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<Order?> GetByIdForUpdateAsync(Guid orderId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task UpdateAsync(Order order, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
}

public sealed class OrderRepository : IOrderRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrderRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task CreateAsync(Order order, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = @"
            INSERT INTO orders (id, status, stock_status, payment_status, amount, currency, customer_id, created_at, updated_at)
            VALUES (@id, @status, @stock_status, @payment_status, @amount, @currency, @customer_id, @created_at, @updated_at);
        ";

        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            Parameters =
            {
                new("id", order.Id),
                new("status", order.Status),
                new("stock_status", order.StockStatus),
                new("payment_status", order.PaymentStatus),
                new("amount", order.Amount),
                new("currency", order.Currency),
                new("customer_id", (object?)order.CustomerId ?? DBNull.Value),
                new("created_at", order.CreatedAt),
                new("updated_at", order.UpdatedAt)
            }
        };

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await GetByIdAsync(orderId, connection, null, cancellationToken);
    }

    public async Task<Order?> GetByIdForUpdateAsync(Guid orderId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        return await GetByIdAsync(orderId, connection, transaction, cancellationToken, true);
    }

    public async Task UpdateAsync(Order order, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE orders
            SET status = @status,
                stock_status = @stock_status,
                payment_status = @payment_status,
                amount = @amount,
                currency = @currency,
                customer_id = @customer_id,
                updated_at = @updated_at
            WHERE id = @id;
        ";

        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            Parameters =
            {
                new("id", order.Id),
                new("status", order.Status),
                new("stock_status", order.StockStatus),
                new("payment_status", order.PaymentStatus),
                new("amount", order.Amount),
                new("currency", order.Currency),
                new("customer_id", (object?)order.CustomerId ?? DBNull.Value),
                new("updated_at", order.UpdatedAt)
            }
        };

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Order?> GetByIdAsync(
        Guid orderId,
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken,
        bool forUpdate = false)
    {
        var sql = @"
            SELECT id, status, stock_status, payment_status, amount, currency, customer_id, created_at, updated_at
            FROM orders
            WHERE id = @id
        ";

        if (forUpdate)
        {
            sql += " FOR UPDATE";
        }

        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            Parameters =
            {
                new("id", orderId)
            }
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Order
        {
            Id = reader.GetGuid(0),
            Status = reader.GetString(1),
            StockStatus = reader.GetString(2),
            PaymentStatus = reader.GetString(3),
            Amount = reader.GetDecimal(4),
            Currency = reader.GetString(5),
            CustomerId = reader.IsDBNull(6) ? null : reader.GetString(6),
            CreatedAt = reader.GetDateTime(7),
            UpdatedAt = reader.GetDateTime(8)
        };
    }
}
