using Catalog.Models;

namespace Catalog.Messaging;

public interface IEventPublisher
{
    Task PublishProductUpsertedAsync(Product product, string requestId, CancellationToken cancellationToken);
}
