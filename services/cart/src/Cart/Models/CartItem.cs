namespace Cart.Models;

public sealed record CartItem
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
