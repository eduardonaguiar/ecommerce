using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace Inventory.Logging;

public sealed class JsonLogFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var payload = new Dictionary<string, object?>
        {
            ["timestamp"] = logEvent.Timestamp.UtcDateTime,
            ["level"] = logEvent.Level.ToString(),
            ["message"] = logEvent.RenderMessage()
        };

        foreach (var property in logEvent.Properties)
        {
            payload[property.Key] = property.Value.ToString().Trim('"');
        }

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        output.WriteLine(json);
    }
}
