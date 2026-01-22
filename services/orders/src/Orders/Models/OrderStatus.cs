namespace Orders.Models;

public static class OrderStatus
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Cancelled = "CANCELLED";
}

public static class StockStatus
{
    public const string Pending = "PENDING";
    public const string Reserved = "RESERVED";
    public const string Failed = "FAILED";
}

public static class PaymentStatus
{
    public const string Pending = "PENDING";
    public const string Processed = "PROCESSED";
}
