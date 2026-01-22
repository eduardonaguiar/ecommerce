namespace Cart.Models;

public sealed record CartModel
{
    public string CartId { get; init; } = string.Empty;
    public IReadOnlyCollection<CartItem> Items { get; init; } = Array.Empty<CartItem>();
}
