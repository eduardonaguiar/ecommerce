using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Catalog.Models;

public sealed record Product
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; init; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; init; }

    [BsonElement("currency")]
    public string Currency { get; init; } = "USD";

    [BsonElement("description")]
    public string? Description { get; init; }

    [BsonElement("image_url")]
    public string? ImageUrl { get; init; }

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
