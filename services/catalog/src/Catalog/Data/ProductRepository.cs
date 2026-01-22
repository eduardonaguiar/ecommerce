using Catalog.Models;
using MongoDB.Driver;

namespace Catalog.Data;

public sealed class ProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _collection;

    public ProductRepository(IMongoCollection<Product> collection)
    {
        _collection = collection;
    }

    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _collection.Find(FilterDefinition<Product>.Empty).ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return _collection.Find(product => product.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var filter = Builders<Product>.Filter.Ne(product => product.Category, string.Empty);
        return _collection.Distinct(product => product.Category, filter).ToListAsync(cancellationToken);
    }

    public async Task<Product> UpsertAsync(Product product, CancellationToken cancellationToken)
    {
        var updated = product with { UpdatedAt = DateTime.UtcNow };
        var filter = Builders<Product>.Filter.Eq(existing => existing.Id, updated.Id);
        await _collection.ReplaceOneAsync(filter, updated, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        return updated;
    }
}
