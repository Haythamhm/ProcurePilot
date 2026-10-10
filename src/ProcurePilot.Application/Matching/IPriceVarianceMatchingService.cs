using ProcurePilot.Domain.Matching;

namespace ProcurePilot.Application.Matching;

public interface IPriceVarianceMatchingService
{
    Task<PriceVarianceMatchOutcome> EvaluateAsync(
        PriceVarianceMatchCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record PriceVarianceMatchCommand(
    Guid InvoiceLineId,
    Guid PurchaseOrderLineId,
    Guid MatchingRuleId);

public sealed record PriceVarianceMatchOutcome(
    Guid MatchResultId,
    MatchResultStatus Status,
    MatchExceptionCode? ExceptionCode,
    Guid? ExceptionCaseId);
