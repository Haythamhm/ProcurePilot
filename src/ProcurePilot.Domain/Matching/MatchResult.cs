using ProcurePilot.Domain.Invoices;
using ProcurePilot.Domain.Purchasing;
using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Matching;

public sealed class MatchResult : ITenantScopedEntity
{
    public const int StatusMaxLength = 16;
    public const int ExceptionCodeMaxLength = 64;
    public const int VarianceScale = 4;

    private MatchResult()
    {
    }

    private MatchResult(
        Guid id,
        Guid tenantId,
        Guid invoiceLineId,
        Guid purchaseOrderLineId,
        MatchingRule rule,
        decimal invoiceUnitPrice,
        decimal purchaseOrderUnitPrice,
        decimal variancePercent,
        MatchResultStatus status,
        MatchExceptionCode? exceptionCode,
        DateTimeOffset evaluatedAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        InvoiceLineId = invoiceLineId;
        PurchaseOrderLineId = purchaseOrderLineId;
        MatchingRuleId = rule.Id;
        MatchingRuleVersion = rule.Version;
        InvoiceUnitPrice = invoiceUnitPrice;
        PurchaseOrderUnitPrice = purchaseOrderUnitPrice;
        AllowedTolerancePercent = rule.AllowedTolerancePercent;
        VariancePercent = variancePercent;
        Status = status;
        ExceptionCode = exceptionCode;
        EvaluatedAtUtc = evaluatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceLineId { get; private set; }

    public Guid PurchaseOrderLineId { get; private set; }

    public Guid MatchingRuleId { get; private set; }

    public int MatchingRuleVersion { get; private set; }

    public decimal InvoiceUnitPrice { get; private set; }

    public decimal PurchaseOrderUnitPrice { get; private set; }

    public decimal AllowedTolerancePercent { get; private set; }

    public decimal VariancePercent { get; private set; }

    public MatchResultStatus Status { get; private set; }

    public MatchExceptionCode? ExceptionCode { get; private set; }

    public DateTimeOffset EvaluatedAtUtc { get; private set; }

    public static MatchResult EvaluatePriceVariance(
        Guid id,
        InvoiceLine invoiceLine,
        PurchaseOrderLine purchaseOrderLine,
        MatchingRule rule,
        DateTimeOffset evaluatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A match result identifier is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(invoiceLine);
        ArgumentNullException.ThrowIfNull(purchaseOrderLine);
        ArgumentNullException.ThrowIfNull(rule);

        if (invoiceLine.TenantId != purchaseOrderLine.TenantId ||
            invoiceLine.TenantId != rule.TenantId)
        {
            throw new InvalidOperationException("Matching inputs must belong to the same tenant.");
        }

        if (rule.Type is not MatchingRuleType.PriceVariance)
        {
            throw new InvalidOperationException("A PRICE_VARIANCE rule is required.");
        }

        var evaluationTime = evaluatedAtUtc.ToUniversalTime();
        if (evaluationTime < rule.EffectiveFromUtc ||
            (rule.EffectiveToUtc.HasValue && evaluationTime >= rule.EffectiveToUtc.Value))
        {
            throw new InvalidOperationException("The matching rule is not effective at the evaluation time.");
        }

        if (purchaseOrderLine.UnitPrice == 0m)
        {
            throw new InvalidOperationException(
                "PRICE_VARIANCE cannot be calculated when the purchase order unit price is zero.");
        }

        var exactVariancePercent =
            (invoiceLine.UnitPrice - purchaseOrderLine.UnitPrice) /
            purchaseOrderLine.UnitPrice * 100m;
        var variancePercent = decimal.Round(
            exactVariancePercent,
            VarianceScale,
            MidpointRounding.AwayFromZero);
        var failed = Math.Abs(exactVariancePercent) > rule.AllowedTolerancePercent;

        return new MatchResult(
            id,
            invoiceLine.TenantId,
            invoiceLine.Id,
            purchaseOrderLine.Id,
            rule,
            invoiceLine.UnitPrice,
            purchaseOrderLine.UnitPrice,
            variancePercent,
            failed ? MatchResultStatus.Failed : MatchResultStatus.Passed,
            failed ? MatchExceptionCode.PriceVariance : null,
            evaluationTime);
    }
}
