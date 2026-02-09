using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using Inventory.Data;
using Inventory.Logging;
using Inventory.Messaging;
using Inventory.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "inventory";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
var postgresConnectionString = builder.Configuration["Postgres:ConnectionString"]
    ?? builder.Configuration["POSTGRES_CONNECTION_STRING"]
    ?? "Host=postgres;Port=5432;Username=ecommerce;Password=ecommerce;Database=inventory";
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]
    ?? "kafka:9092";
var kafkaInventoryTopic = builder.Configuration["Kafka:InventoryTopic"]
    ?? builder.Configuration["KAFKA_INVENTORY_TOPIC"]
    ?? "inventory.events";
var kafkaGroupId = builder.Configuration["Kafka:GroupId"]
    ?? builder.Configuration["KAFKA_GROUP_ID"]
    ?? "inventory-saga";

var inventorySettings = new InventorySettings
{
    DefaultProductId = builder.Configuration["Inventory:DefaultProductId"]
        ?? builder.Configuration["INVENTORY_DEFAULT_PRODUCT_ID"]
        ?? "default",
    DefaultStock = int.TryParse(
            builder.Configuration["Inventory:DefaultStock"]
            ?? builder.Configuration["INVENTORY_DEFAULT_STOCK"],
            out var stock)
        ? stock
        : 100,
    DefaultReservationQuantity = int.TryParse(
            builder.Configuration["Inventory:DefaultReservationQuantity"]
            ?? builder.Configuration["INVENTORY_DEFAULT_RESERVATION_QTY"],
            out var qty)
        ? qty
        : 1
};

builder.Host.UseSerilog((_, _, loggerConfig) =>
{
    loggerConfig
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", serviceName)
        .Enrich.WithProperty("env", serviceEnv)
        .WriteTo.Console(new JsonLogFormatter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Inventory Service",
        Version = "v1",
        Description = "Inventory reservations backed by Postgres and Kafka."
    });
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(serviceName)
            .SetSampler(new AlwaysOnSampler())
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter(serviceName)
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

builder.Services.AddSingleton(new ActivitySource(serviceName));
builder.Services.AddSingleton(new Meter(serviceName));
builder.Services.AddSingleton(inventorySettings);
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(postgresConnectionString));
builder.Services.AddSingleton<IInventoryRepository, InventoryRepository>();
builder.Services.AddSingleton<InventorySchemaInitializer>();
builder.Services.AddSingleton<InventorySagaHandler>();

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    Acks = Acks.All
};

builder.Services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(producerConfig).Build());
builder.Services.AddSingleton<IInventoryEventPublisher>(sp =>
    new KafkaInventoryEventPublisher(sp.GetRequiredService<IProducer<string, string>>(), kafkaInventoryTopic, serviceName));

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    GroupId = kafkaGroupId,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false
};

builder.Services.AddSingleton<IConsumer<string, string>>(_ => new ConsumerBuilder<string, string>(consumerConfig).Build());
builder.Services.AddHostedService<InventorySagaConsumer>();

var app = builder.Build();

await app.Services.GetRequiredService<InventorySchemaInitializer>().InitializeAsync(CancellationToken.None);

app.Use(async (context, next) =>
{
    using (LogContext.PushProperty("request_id", context.TraceIdentifier))
    {
        await next();
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new { status = "ok", service = serviceName }))
    .WithName("GetRoot")
    .WithOpenApi();

app.MapGet("/inventory/{productId}", async (
        string productId,
        IInventoryRepository repository,
        CancellationToken cancellationToken) =>
    {
        var item = await repository.GetStockItemAsync(productId, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(InventoryItemResponse.FromItem(item));
    })
    .WithName("GetInventory")
    .WithOpenApi();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    var producer = app.Services.GetRequiredService<IProducer<string, string>>();
    producer.Flush(TimeSpan.FromSeconds(5));
    producer.Dispose();
});

app.Run();
