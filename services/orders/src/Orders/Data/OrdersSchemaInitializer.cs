using Npgsql;
using Serilog.Context;

namespace Orders.Data;

public sealed class OrdersSchemaInitializer
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<OrdersSchemaInitializer> _logger;

    public OrdersSchemaInitializer(NpgsqlDataSource dataSource, ILogger<OrdersSchemaInitializer> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS orders (
                id UUID PRIMARY KEY,
                status TEXT NOT NULL,
                stock_status TEXT NOT NULL,
                payment_status TEXT NOT NULL,
                amount NUMERIC(12, 2) NOT NULL,
                currency TEXT NOT NULL,
                customer_id TEXT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
        ";

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        using (LogContext.PushProperty("request_id", "startup"))
        {
            _logger.LogInformation("{event} ensured orders schema", "orders.schema.ready");
        }
    }
}
