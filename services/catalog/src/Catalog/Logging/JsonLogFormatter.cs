using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace Catalog.Logging;

public sealed class JsonLogFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var activity = System.Diagnostics.Activity.Current;
        var traceId = activity?.TraceId.ToString() ?? string.Empty;
        var spanId = activity?.SpanId.ToString() ?? string.Empty;

        var payload = new Dictionary<string, object?>
        {
            ["timestamp"] = logEvent.Timestamp.UtcDateTime.ToString("O"),
            ["level"] = logEvent.Level.ToString().ToLowerInvariant(),
            ["service"] = GetScalar(logEvent, "service") ?? string.Empty,
            ["env"] = GetScalar(logEvent, "env") ?? string.Empty,
            ["message"] = logEvent.RenderMessage(),
            ["trace_id"] = traceId,
            ["span_id"] = spanId,
            ["request_id"] = GetScalar(logEvent, "request_id") ?? string.Empty
        };

        if (logEvent.Properties.TryGetValue("event", out var eventValue))
        {
            payload["event"] = ConvertValue(eventValue);
        }

        if (logEvent.Properties.TryGetValue("entity_id", out var entityValue))
        {
            payload["entity_id"] = ConvertValue(entityValue);
        }

        if (logEvent.Properties.TryGetValue("duration_ms", out var durationValue))
        {
            payload["duration_ms"] = ConvertValue(durationValue);
        }

        if (logEvent.Exception is not null)
        {
            payload["error"] = new
            {
                type = logEvent.Exception.GetType().FullName,
                message = logEvent.Exception.Message,
                stack = logEvent.Exception.StackTrace
            };
        }

        var attrs = new Dictionary<string, object?>();
        foreach (var property in logEvent.Properties)
        {
            if (IsReserved(property.Key))
            {
                continue;
            }

            attrs[property.Key] = ConvertValue(property.Value);
        }

        if (attrs.Count > 0)
        {
            payload["attrs"] = attrs;
        }

        output.WriteLine(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private static bool IsReserved(string propertyName)
    {
        return propertyName is "service" or "env" or "event" or "entity_id" or "duration_ms" or "request_id";
    }

    private static object? GetScalar(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value)
            ? ConvertValue(value)
            : null;
    }

    private static object? ConvertValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            SequenceValue sequence => sequence.Elements.Select(ConvertValue).ToArray(),
            DictionaryValue dictionary => dictionary.Elements.ToDictionary(
                element => element.Key.Value?.ToString() ?? string.Empty,
                element => ConvertValue(element.Value)),
            StructureValue structure => structure.Properties.ToDictionary(
                property => property.Name,
                property => ConvertValue(property.Value)),
            _ => value.ToString()
        };
    }
}
