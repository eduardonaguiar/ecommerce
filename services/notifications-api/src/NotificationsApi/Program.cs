using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using Serilog;
using Serilog.Context;
using NotificationsApi.Logging;
using NotificationsApi.Messaging;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "notifications-api";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]
    ?? "kafka:9092";
var kafkaTopic = builder.Configuration["Kafka:Topic"]
    ?? builder.Configuration["KAFKA_TOPIC"]
    ?? "orders.events";
var kafkaGroupId = builder.Configuration["Kafka:GroupId"]
    ?? builder.Configuration["KAFKA_GROUP_ID"]
    ?? "notifications-api";

var rabbitOptions = new RabbitMqOptions
{
    HostName = builder.Configuration["RabbitMq:Host"]
        ?? builder.Configuration["RABBITMQ_HOST"]
        ?? "rabbitmq",
    Port = int.TryParse(builder.Configuration["RabbitMq:Port"]
        ?? builder.Configuration["RABBITMQ_PORT"], out var port)
        ? port
        : 5672,
    UserName = builder.Configuration["RabbitMq:UserName"]
        ?? builder.Configuration["RABBITMQ_USERNAME"]
        ?? "ecommerce",
    Password = builder.Configuration["RabbitMq:Password"]
        ?? builder.Configuration["RABBITMQ_PASSWORD"]
        ?? "ecommerce",
    QueueName = builder.Configuration["RabbitMq:QueueName"]
        ?? builder.Configuration["RABBITMQ_QUEUE"]
        ?? "notifications.send"
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
        Title = "Notifications API",
        Version = "v1",
        Description = "Consumes order events and enqueues notification jobs."
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

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    GroupId = kafkaGroupId,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false
};

builder.Services.AddSingleton<IConsumer<string, string>>(_ => new ConsumerBuilder<string, string>(consumerConfig).Build());

builder.Services.AddSingleton(rabbitOptions);
builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        HostName = rabbitOptions.HostName,
        Port = rabbitOptions.Port,
        UserName = rabbitOptions.UserName,
        Password = rabbitOptions.Password,
        DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true
    };

    return factory.CreateConnection();
});

builder.Services.AddSingleton<NotificationQueuePublisher>();
builder.Services.AddHostedService<NotificationEventConsumer>();

var app = builder.Build();

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
    var connection = app.Services.GetRequiredService<IConnection>();
    connection.Close();
    connection.Dispose();

    var consumer = app.Services.GetRequiredService<IConsumer<string, string>>();
    consumer.Close();
    consumer.Dispose();
});

app.Run();
