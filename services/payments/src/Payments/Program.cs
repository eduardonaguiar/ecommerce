using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payments.Data;
using Payments.Logging;
using Payments.Messaging;
using Payments.Models;
using Payments.Processing;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "payments";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
var postgresConnectionString = builder.Configuration["Postgres:ConnectionString"]
    ?? builder.Configuration["POSTGRES_CONNECTION_STRING"]
    ?? "Host=postgres;Port=5432;Username=ecommerce;Password=ecommerce;Database=payments";
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]
    ?? "kafka:9092";
var kafkaTopic = builder.Configuration["Kafka:Topic"]
    ?? builder.Configuration["KAFKA_TOPIC"]
    ?? "payments.events";

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
        Title = "Payments Service",
        Version = "v1",
        Description = "Deterministic payment processing backed by Postgres and Kafka."
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
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<PaymentsSchemaInitializer>();
builder.Services.AddSingleton<PaymentDecisionEngine>();

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    Acks = Acks.All
};

builder.Services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(producerConfig).Build());
builder.Services.AddSingleton<IPaymentEventPublisher>(sp =>
    new KafkaPaymentEventPublisher(sp.GetRequiredService<IProducer<string, string>>(), kafkaTopic, serviceName));

var app = builder.Build();

await app.Services.GetRequiredService<PaymentsSchemaInitializer>().InitializeAsync(CancellationToken.None);

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

app.MapGet("/payments/{paymentId:guid}", async (
        Guid paymentId,
        IPaymentRepository repository,
        CancellationToken cancellationToken) =>
    {
        var attempt = await repository.GetAttemptByIdAsync(paymentId, cancellationToken);
        return attempt is null ? Results.NotFound() : Results.Ok(PaymentResponse.FromAttempt(attempt));
    })
    .WithName("GetPayment");    

app.MapPost("/payments", async (
        PaymentRequest request,
        IPaymentRepository repository,
        PaymentDecisionEngine decisionEngine,
        NpgsqlDataSource dataSource,
        IPaymentEventPublisher publisher,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (request.OrderId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "orderId is required." });
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest(new { error = "amount must be greater than 0." });
        }

        if (!decisionEngine.TryDecide(request, out var decision, out var error))
        {
            return Results.BadRequest(new { error });
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Amount = request.Amount,
            Currency = currency,
            Status = decision.IsSuccess ? PaymentStatus.Success : PaymentStatus.Failure,
            FailureReason = decision.FailureReason,
            CreatedAt = now
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await repository.CreateAttemptAsync(attempt, connection, transaction, cancellationToken);

        if (decision.IsSuccess)
        {
            var effective = new EffectivePayment
            {
                Id = attempt.Id,
                OrderId = attempt.OrderId,
                Amount = attempt.Amount,
                Currency = attempt.Currency,
                ProcessedAt = now
            };

            await repository.CreateEffectiveAsync(effective, connection, transaction, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        await publisher.PublishPaymentProcessedAsync(attempt, httpContext.TraceIdentifier, cancellationToken);

        var logger = loggerFactory.CreateLogger("PaymentsApi");
        logger.LogInformation(
            "{event} processed payment {payment_id} {order_id} {status}",
            "payment.processed",
            attempt.Id,
            attempt.OrderId,
            attempt.Status);

        return Results.Ok(PaymentResponse.FromAttempt(attempt));
    })
    .WithName("ProcessPayment");

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
