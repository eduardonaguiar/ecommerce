namespace Orders.Models;

public static class OrderStateMachine
{
    public static OrderTransition ApplyStockReserved(Order order)
    {
        if (IsTerminal(order))
        {
            return OrderTransition.NoChange(order);
        }

        var newStatus = order.PaymentStatus == PaymentStatus.Paid
            ? OrderStatus.Confirmed
            : order.Status;

        return BuildTransition(order, newStatus, StockStatus.Reserved, order.PaymentStatus, "stock.reserved");
    }

    public static OrderTransition ApplyStockFailed(Order order, string? reason)
    {
        if (order.Status == OrderStatus.Cancelled)
        {
            return OrderTransition.NoChange(order);
        }

        return BuildTransition(order, OrderStatus.Cancelled, StockStatus.OutOfStock, order.PaymentStatus, "stock.failed", reason);
    }

    public static OrderTransition ApplyPaymentProcessed(Order order)
    {
        if (IsTerminal(order))
        {
            return OrderTransition.NoChange(order);
        }

        var newStatus = order.StockStatus switch
        {
            StockStatus.Reserved => OrderStatus.Confirmed,
            StockStatus.OutOfStock => OrderStatus.Cancelled,
            _ => order.Status
        };

        return BuildTransition(order, newStatus, order.StockStatus, PaymentStatus.Paid, "payment.processed");
    }

    private static bool IsTerminal(Order order)
    {
        return order.Status is OrderStatus.Confirmed or OrderStatus.Cancelled;
    }

    private static OrderTransition BuildTransition(
        Order order,
        OrderStatus newStatus,
        StockStatus newStockStatus,
        PaymentStatus newPaymentStatus,
        string trigger,
        string? cancelReason = null)
    {
        var hasChange = newStatus != order.Status
            || newStockStatus != order.StockStatus
            || newPaymentStatus != order.PaymentStatus;

        return new OrderTransition(
            hasChange,
            newStatus,
            newStockStatus,
            newPaymentStatus,
            newStatus == OrderStatus.Confirmed && order.Status != OrderStatus.Confirmed,
            newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled,
            trigger,
            cancelReason);
    }
}

public sealed record OrderTransition(
    bool HasChange,
    OrderStatus Status,
    StockStatus StockStatus,
    PaymentStatus PaymentStatus,
    bool PublishConfirmed,
    bool PublishCancelled,
    string Trigger,
    string? CancelReason)
{
    public static OrderTransition NoChange(Order order) => new(
        false,
        order.Status,
        order.StockStatus,
        order.PaymentStatus,
        false,
        false,
        "noop",
        null);
}
