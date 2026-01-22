using Npgsql;
using Payments.Models;

namespace Payments.Data;

public interface IPaymentRepository
{
    Task CreateAttemptAsync(PaymentAttempt attempt, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task CreateEffectiveAsync(EffectivePayment payment, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
    Task<PaymentAttempt?> GetAttemptByIdAsync(Guid paymentId, CancellationToken cancellationToken);
}

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PaymentRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task CreateAttemptAsync(
        PaymentAttempt attempt,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO payment_attempts (id, order_id, amount, currency, status, failure_reason, created_at)
            VALUES (@id, @order_id, @amount, @currency, @status, @failure_reason, @created_at);
        ";

        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            Parameters =
            {
                new("id", attempt.Id),
                new("order_id", attempt.OrderId),
                new("amount", attempt.Amount),
                new("currency", attempt.Currency),
                new("status", attempt.Status),
                new("failure_reason", (object?)attempt.FailureReason ?? DBNull.Value),
                new("created_at", attempt.CreatedAt)
            }
        };

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CreateEffectiveAsync(
        EffectivePayment payment,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO effective_payments (id, order_id, amount, currency, processed_at)
            VALUES (@id, @order_id, @amount, @currency, @processed_at);
        ";

        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            Parameters =
            {
                new("id", payment.Id),
                new("order_id", payment.OrderId),
                new("amount", payment.Amount),
                new("currency", payment.Currency),
                new("processed_at", payment.ProcessedAt)
            }
        };

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PaymentAttempt?> GetAttemptByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT id, order_id, amount, currency, status, failure_reason, created_at
            FROM payment_attempts
            WHERE id = @id;
        ";

        await using var command = new NpgsqlCommand(sql, connection)
        {
            Parameters =
            {
                new("id", paymentId)
            }
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PaymentAttempt
        {
            Id = reader.GetGuid(0),
            OrderId = reader.GetGuid(1),
            Amount = reader.GetDecimal(2),
            Currency = reader.GetString(3),
            Status = reader.GetString(4),
            FailureReason = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = reader.GetDateTime(6)
        };
    }
}
