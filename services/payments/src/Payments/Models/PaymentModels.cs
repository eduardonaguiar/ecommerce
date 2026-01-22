namespace Payments.Models;

public static class PaymentStatus
{
    public const string Success = "SUCCESS";
    public const string Failure = "FAILURE";
}

public sealed record PaymentAttempt
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Status { get; init; } = PaymentStatus.Failure;
    public string? FailureReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record EffectivePayment
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime ProcessedAt { get; init; }
}

public sealed record PaymentRequest
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? ForceOutcome { get; init; }
}

public sealed record PaymentResponse
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Status { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public bool Effective { get; init; }
    public DateTime CreatedAt { get; init; }

    public static PaymentResponse FromAttempt(PaymentAttempt attempt)
    {
        return new PaymentResponse
        {
            Id = attempt.Id,
            OrderId = attempt.OrderId,
            Amount = attempt.Amount,
            Currency = attempt.Currency,
            Status = attempt.Status,
            FailureReason = attempt.FailureReason,
            Effective = attempt.Status == PaymentStatus.Success,
            CreatedAt = attempt.CreatedAt
        };
    }
}
