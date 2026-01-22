using Catalog.Models;

namespace Catalog.Data;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<Product> UpsertAsync(Product product, CancellationToken cancellationToken);
}
