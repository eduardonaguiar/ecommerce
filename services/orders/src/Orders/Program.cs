using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orders.Data;
using Orders.Logging;
using Orders.Messaging;
using Orders.Models;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "orders";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
var postgresConnectionString = builder.Configuration["Postgres:ConnectionString"]
    ?? builder.Configuration["POSTGRES_CONNECTION_STRING"]
    ?? "Host=postgres;Port=5432;Username=ecommerce;Password=ecommerce;Database=orders";
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]
    ?? "kafka:9092";
var kafkaTopic = builder.Configuration["Kafka:Topic"]
    ?? builder.Configuration["KAFKA_TOPIC"]
    ?? "orders.events";
var kafkaGroupId = builder.Configuration["Kafka:GroupId"]
    ?? builder.Configuration["KAFKA_GROUP_ID"]
    ?? "orders-saga";

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
        Title = "Orders Service",
        Version = "v1",
        Description = "Saga orchestrator for the order lifecycle backed by Postgres."
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
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(postgresConnectionString));
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<OrdersSchemaInitializer>();
builder.Services.AddSingleton<OrderSagaHandler>();

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    Acks = Acks.All
};

builder.Services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(producerConfig).Build());
builder.Services.AddSingleton<IOrderEventPublisher>(sp =>
    new KafkaOrderEventPublisher(sp.GetRequiredService<IProducer<string, string>>(), kafkaTopic, serviceName));

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    GroupId = kafkaGroupId,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false
};

builder.Services.AddSingleton<IConsumer<string, string>>(_ => new ConsumerBuilder<string, string>(consumerConfig).Build());
builder.Services.AddHostedService<OrderSagaConsumer>();

var app = builder.Build();

await app.Services.GetRequiredService<OrdersSchemaInitializer>().InitializeAsync(CancellationToken.None);

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
    .WithName("GetRoot");

app.MapGet("/orders/{orderId:guid}", async (Guid orderId, IOrderRepository repository, CancellationToken cancellationToken) =>
{
    var order = await repository.GetByIdAsync(orderId, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(OrderResponse.FromOrder(order));
})
    .WithName("GetOrder");    

app.MapPost("/orders", async (
        OrderCreateRequest request,
        IOrderRepository repository,
        IOrderEventPublisher publisher,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (request.Amount <= 0)
        {
            return Results.BadRequest(new { error = "amount must be greater than 0." });
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            StockStatus = StockStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            Amount = request.Amount,
            Currency = currency,
            CustomerId = string.IsNullOrWhiteSpace(request.CustomerId) ? null : request.CustomerId.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(order, cancellationToken);
        await publisher.PublishOrderCreatedAsync(order, httpContext.TraceIdentifier, cancellationToken);

        var logger = loggerFactory.CreateLogger("OrdersApi");
        logger.LogInformation("{event} created order {order_id} {status}", "order.created", order.Id, order.Status);

        return Results.Accepted($"/orders/{order.Id}", OrderResponse.FromOrder(order));
    })
    .WithName("CreateOrder");    

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
