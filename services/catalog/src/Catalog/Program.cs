using Catalog.Data;
using Catalog.Logging;
using Catalog.Messaging;
using Catalog.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "catalog";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"]
    ?? builder.Configuration["MONGO_CONNECTION_STRING"]
    ?? "mongodb://ecommerce:ecommerce@mongodb:27017/catalog?authSource=admin";
var mongoDatabase = builder.Configuration["Mongo:Database"]
    ?? builder.Configuration["MONGO_DATABASE"]
    ?? "catalog";
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]
    ?? "kafka:9092";
var kafkaTopic = builder.Configuration["Kafka:Topic"]
    ?? builder.Configuration["KAFKA_TOPIC"]
    ?? "catalog.product-upserted";

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
        Title = "Catalog Service",
        Version = "v1",
        Description = "Product and category catalog backed by MongoDB."
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
            .SetSampler(new AlwaysOnSampler())
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoDatabase>().GetCollection<Product>("products"));
builder.Services.AddSingleton<IProductRepository, ProductRepository>();

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    Acks = Acks.All
};

builder.Services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(producerConfig).Build());
builder.Services.AddSingleton<IEventPublisher>(sp =>
    new KafkaEventPublisher(sp.GetRequiredService<IProducer<string, string>>(), kafkaTopic, serviceName));

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

app.MapGet("/products", async (IProductRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAllAsync(cancellationToken)))
    .WithName("GetProducts")
    .WithOpenApi();

app.MapGet("/products/{id}", async (string id, IProductRepository repository, CancellationToken cancellationToken) =>
{
    var product = await repository.GetByIdAsync(id, cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
    .WithName("GetProductById")
    .WithOpenApi();

app.MapGet("/categories", async (IProductRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetCategoriesAsync(cancellationToken)))
    .WithName("GetCategories")
    .WithOpenApi();

app.MapPost("/admin/products", async (
        ProductUpsertRequest request,
        IProductRepository repository,
        IEventPublisher publisher,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
        {
            return Results.BadRequest(new { error = "name and category are required." });
        }

        var productId = string.IsNullOrWhiteSpace(request.Id)
            ? Guid.NewGuid().ToString("N")
            : request.Id.Trim();

        var product = new Product
        {
            Id = productId,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim(),
            Description = request.Description,
            ImageUrl = request.ImageUrl
        };

        var saved = await repository.UpsertAsync(product, cancellationToken);
        await publisher.PublishProductUpsertedAsync(saved, httpContext.TraceIdentifier, cancellationToken);

        return Results.Ok(saved);
    })
    .WithName("UpsertProduct")
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
