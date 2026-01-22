using System.Collections.Concurrent;
using Cart.Models;

namespace Cart.Data;

public sealed class InMemoryCartRepository : ICartRepository
{
    private readonly ConcurrentDictionary<string, CartState> _carts = new(StringComparer.OrdinalIgnoreCase);

    public CartModel? GetCart(string cartId)
    {
        if (!_carts.TryGetValue(cartId, out var state))
        {
            return null;
        }

        lock (state.Lock)
        {
            return new CartModel
            {
                CartId = cartId,
                Items = state.Items.Values.Select(item => new CartItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToArray()
            };
        }
    }

    public CartModel UpsertItem(string cartId, string productId, int quantity)
    {
        var state = _carts.GetOrAdd(cartId, _ => new CartState());

        lock (state.Lock)
        {
            if (quantity == 0)
            {
                state.Items.Remove(productId);
            }
            else
            {
                state.Items[productId] = new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity
                };
            }

            return new CartModel
            {
                CartId = cartId,
                Items = state.Items.Values.Select(item => new CartItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToArray()
            };
        }
    }

    public CartModel RemoveItem(string cartId, string productId)
    {
        var state = _carts.GetOrAdd(cartId, _ => new CartState());

        lock (state.Lock)
        {
            state.Items.Remove(productId);

            return new CartModel
            {
                CartId = cartId,
                Items = state.Items.Values.Select(item => new CartItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToArray()
            };
        }
    }

    private sealed class CartState
    {
        public object Lock { get; } = new();
        public Dictionary<string, CartItem> Items { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
