namespace Orders.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Cancelled
}

public enum StockStatus
{
    Pending,
    Reserved,
    OutOfStock
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed
}
