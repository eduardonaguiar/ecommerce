using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using Serilog;
using Serilog.Context;
using NotificationsWorker.Logging;
using NotificationsWorker.Messaging;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "notifications-worker";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";

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

var workerOptions = new NotificationWorkerOptions
{
    MaxRetries = int.TryParse(builder.Configuration["Notifications:MaxRetries"]
        ?? builder.Configuration["NOTIFICATIONS_MAX_RETRIES"], out var maxRetries)
        ? maxRetries
        : 3,
    BaseDelaySeconds = int.TryParse(builder.Configuration["Notifications:RetryDelaySeconds"]
        ?? builder.Configuration["NOTIFICATIONS_RETRY_DELAY_SECONDS"], out var delaySeconds)
        ? delaySeconds
        : 2,
    SimulatedFailureRate = double.TryParse(builder.Configuration["Notifications:SimulatedFailureRate"]
        ?? builder.Configuration["NOTIFICATIONS_SIMULATED_FAILURE_RATE"], out var failureRate)
        ? failureRate
        : 0,
    PrefetchCount = ushort.TryParse(builder.Configuration["RabbitMq:Prefetch"]
        ?? builder.Configuration["RABBITMQ_PREFETCH"], out var prefetch)
        ? prefetch
        : (ushort)4
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
        Title = "Notifications Worker",
        Version = "v1",
        Description = "Processes notification jobs from RabbitMQ."
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

builder.Services.AddSingleton(rabbitOptions);
builder.Services.AddSingleton(workerOptions);
builder.Services.AddSingleton(_ => new ConnectionFactory
{
    HostName = rabbitOptions.HostName,
    Port = rabbitOptions.Port,
    UserName = rabbitOptions.UserName,
    Password = rabbitOptions.Password,
    DispatchConsumersAsync = true,
    AutomaticRecoveryEnabled = true
});

builder.Services.AddHostedService<NotificationWorker>();

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

app.Run();
