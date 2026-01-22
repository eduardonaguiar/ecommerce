using System.Text.Json.Serialization;

namespace Catalog.Messaging;

public sealed record EventEnvelope<T>
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; init; } = string.Empty;

    [JsonPropertyName("trace_id")]
    public string? TraceId { get; init; }

    [JsonPropertyName("span_id")]
    public string? SpanId { get; init; }

    [JsonPropertyName("request_id")]
    public string RequestId { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1";

    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;
}
