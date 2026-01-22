using Cart.Models;

namespace Cart.Data;

public interface ICartRepository
{
    CartModel? GetCart(string cartId);
    CartModel UpsertItem(string cartId, string productId, int quantity);
    CartModel RemoveItem(string cartId, string productId);
}
