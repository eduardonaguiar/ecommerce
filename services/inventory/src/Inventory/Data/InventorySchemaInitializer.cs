using Inventory.Models;
using Npgsql;
using Serilog.Context;

namespace Inventory.Data;

public sealed class InventorySchemaInitializer
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly InventorySettings _settings;
    private readonly ILogger<InventorySchemaInitializer> _logger;

    public InventorySchemaInitializer(NpgsqlDataSource dataSource, InventorySettings settings, ILogger<InventorySchemaInitializer> logger)
    {
        _dataSource = dataSource;
        _settings = settings;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS stock_items (
                product_id TEXT PRIMARY KEY,
                available_quantity INT NOT NULL,
                reserved_quantity INT NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS stock_reservations (
                id UUID PRIMARY KEY,
                order_id UUID NOT NULL UNIQUE,
                product_id TEXT NOT NULL,
                quantity INT NOT NULL,
                status TEXT NOT NULL,
                failure_reason TEXT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
        ";

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        const string seedSql = @"
            INSERT INTO stock_items (product_id, available_quantity, reserved_quantity, updated_at)
            VALUES (@product_id, @available_quantity, 0, @updated_at)
            ON CONFLICT (product_id) DO NOTHING;";
        await using var seedCommand = new NpgsqlCommand(seedSql, connection);
        seedCommand.Parameters.AddWithValue("product_id", _settings.DefaultProductId);
        seedCommand.Parameters.AddWithValue("available_quantity", _settings.DefaultStock);
        seedCommand.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
        await seedCommand.ExecuteNonQueryAsync(cancellationToken);

        using (LogContext.PushProperty("request_id", "startup"))
        {
            _logger.LogInformation(
                "{event} ensured inventory schema and seed data {product_id}",
                "inventory.schema.ready",
                _settings.DefaultProductId);
        }
    }
}
