using System.Diagnostics.Metrics;

namespace Cart.Observability;

public sealed class CartMetrics
{
    private readonly Counter<long> _itemUpserted;
    private readonly Counter<long> _itemRemoved;

    public CartMetrics(Meter meter)
    {
        _itemUpserted = meter.CreateCounter<long>("cart_item_upsert_total");
        _itemRemoved = meter.CreateCounter<long>("cart_item_removed_total");
    }

    public void RecordItemUpserted()
    {
        _itemUpserted.Add(1);
    }

    public void RecordItemRemoved()
    {
        _itemRemoved.Add(1);
    }
}
