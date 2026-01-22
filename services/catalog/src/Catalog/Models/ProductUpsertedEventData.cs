namespace Catalog.Models;

public sealed record ProductUpsertedEventData
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime UpdatedAt { get; init; }
}
