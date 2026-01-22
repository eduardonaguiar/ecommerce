namespace Orders.Messaging;

public static class OrderEventTypes
{
    public const string OrderCreated = "order.created";
    public const string OrderConfirmed = "order.confirmed";
    public const string OrderCancelled = "order.cancelled";
    public const string StockReserved = "stock.reserved";
    public const string StockFailed = "stock.failed";
    public const string PaymentProcessed = "payment.processed";
}
