using Payments.Models;

namespace Payments.Processing;

public sealed record PaymentDecision(bool IsSuccess, string? FailureReason);

public sealed class PaymentDecisionEngine
{
    private static readonly string[] AllowedOutcomes = ["success", "failure"];

    public bool TryDecide(PaymentRequest request, out PaymentDecision decision, out string? error)
    {
        error = null;
        decision = new PaymentDecision(false, "mock_declined");

        if (!string.IsNullOrWhiteSpace(request.ForceOutcome))
        {
            var normalized = request.ForceOutcome.Trim().ToLowerInvariant();
            if (!AllowedOutcomes.Contains(normalized))
            {
                error = "forceOutcome must be 'success' or 'failure' when provided.";
                return false;
            }

            decision = normalized == "success"
                ? new PaymentDecision(true, null)
                : new PaymentDecision(false, "mock_declined");
            return true;
        }

        var checksum = request.OrderId.ToByteArray().Sum(value => value);
        var isSuccess = checksum % 2 == 0;
        decision = isSuccess
            ? new PaymentDecision(true, null)
            : new PaymentDecision(false, "mock_declined");
        return true;
    }
}
