using System.Diagnostics;
using Cart.Data;
using Cart.Logging;
using Cart.Models;
using Cart.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["SERVICE_NAME"] ?? "cart";
var serviceEnv = builder.Configuration["SERVICE_ENV"] ?? builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";

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
        Title = "Cart Service",
        Version = "v1",
        Description = "In-memory cart service keyed by cartId."
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

builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
builder.Services.AddSingleton(new ActivitySource(serviceName));
builder.Services.AddSingleton(new Meter(serviceName));
builder.Services.AddSingleton<CartMetrics>();

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

app.MapGet("/carts/{cartId}", (
        string cartId,
        ICartRepository repository,
        ActivitySource activitySource,
        ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("CartEndpoints");
        var normalizedCartId = cartId.Trim();

        using var activity = activitySource.StartActivity("cart.get", ActivityKind.Internal);
        activity?.SetTag("cart.id", normalizedCartId);

        var cart = repository.GetCart(normalizedCartId) ?? new CartModel
        {
            CartId = normalizedCartId,
            Items = Array.Empty<CartItem>()
        };

        logger.LogInformation("{event} Retrieved cart {cart_id} with {item_count} items", "cart_retrieved", normalizedCartId, cart.Items.Count);

        return Results.Ok(cart);
    })
    .WithName("GetCart")
    .WithOpenApi();

app.MapPost("/carts/{cartId}/items", (
        string cartId,
        AddCartItemRequest request,
        ICartRepository repository,
        CartMetrics metrics,
        ActivitySource activitySource,
        ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("CartEndpoints");
        var normalizedCartId = cartId.Trim();
        var normalizedProductId = request.ProductId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedProductId))
        {
            return Results.BadRequest(new { error = "productId is required." });
        }

        if (request.Quantity < 0)
        {
            return Results.BadRequest(new { error = "quantity must be a positive integer or zero to remove." });
        }

        using var activity = activitySource.StartActivity("cart.item.upsert", ActivityKind.Internal);
        activity?.SetTag("cart.id", normalizedCartId);
        activity?.SetTag("cart.item.product_id", normalizedProductId);
        activity?.SetTag("cart.item.quantity", request.Quantity);

        if (request.Quantity == 0)
        {
            var removedCart = repository.RemoveItem(normalizedCartId, normalizedProductId);
            metrics.RecordItemRemoved();
            logger.LogInformation("{event} Removed item {product_id} from cart {cart_id}", "cart_item_removed", normalizedProductId, normalizedCartId);
            return Results.Ok(removedCart);
        }

        var updatedCart = repository.UpsertItem(normalizedCartId, normalizedProductId, request.Quantity);
        metrics.RecordItemUpserted();
        logger.LogInformation(
            "{event} Upserted item {product_id} in cart {cart_id} with quantity {quantity}",
            "cart_item_upserted",
            normalizedProductId,
            normalizedCartId,
            request.Quantity);

        return Results.Ok(updatedCart);
    })
    .WithName("AddCartItem")
    .WithOpenApi();

app.MapDelete("/carts/{cartId}/items/{productId}", (
        string cartId,
        string productId,
        ICartRepository repository,
        CartMetrics metrics,
        ActivitySource activitySource,
        ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("CartEndpoints");
        var normalizedCartId = cartId.Trim();
        var normalizedProductId = productId.Trim();

        if (string.IsNullOrWhiteSpace(normalizedProductId))
        {
            return Results.BadRequest(new { error = "productId is required." });
        }

        using var activity = activitySource.StartActivity("cart.item.remove", ActivityKind.Internal);
        activity?.SetTag("cart.id", normalizedCartId);
        activity?.SetTag("cart.item.product_id", normalizedProductId);

        var updatedCart = repository.RemoveItem(normalizedCartId, normalizedProductId);
        metrics.RecordItemRemoved();
        logger.LogInformation("{event} Removed item {product_id} from cart {cart_id}", "cart_item_removed", normalizedProductId, normalizedCartId);

        return Results.Ok(updatedCart);
    })
    .WithName("RemoveCartItem")
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
