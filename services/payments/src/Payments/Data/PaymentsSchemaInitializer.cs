using Npgsql;
using Serilog.Context;

namespace Payments.Data;

public sealed class PaymentsSchemaInitializer
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PaymentsSchemaInitializer> _logger;

    public PaymentsSchemaInitializer(NpgsqlDataSource dataSource, ILogger<PaymentsSchemaInitializer> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS payment_attempts (
                id UUID PRIMARY KEY,
                order_id UUID NOT NULL,
                amount NUMERIC(12, 2) NOT NULL,
                currency TEXT NOT NULL,
                status TEXT NOT NULL,
                failure_reason TEXT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS effective_payments (
                id UUID PRIMARY KEY,
                order_id UUID NOT NULL,
                amount NUMERIC(12, 2) NOT NULL,
                currency TEXT NOT NULL,
                processed_at TIMESTAMPTZ NOT NULL
            );
        ";

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        using (LogContext.PushProperty("request_id", "startup"))
        {
            _logger.LogInformation("{event} ensured payments schema", "payments.schema.ready");
        }
    }
}
