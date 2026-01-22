namespace Cart.Models;

public sealed record AddCartItemRequest
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
