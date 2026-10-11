using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Exceptions;

public sealed class ExceptionCaseEvidence : ITenantScopedEntity
{
    public const int TypeMaxLength = 32;

    private ExceptionCaseEvidence()
    {
    }

    private ExceptionCaseEvidence(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        ExceptionEvidenceType type,
        Guid? invoiceLineId,
        Guid? purchaseOrderLineId,
        Guid? matchResultId,
        Guid? matchingRuleId,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        ExceptionCaseId = exceptionCaseId;
        Type = type;
        InvoiceLineId = invoiceLineId;
        PurchaseOrderLineId = purchaseOrderLineId;
        MatchResultId = matchResultId;
        MatchingRuleId = matchingRuleId;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ExceptionCaseId { get; private set; }

    public ExceptionEvidenceType Type { get; private set; }

    public Guid? InvoiceLineId { get; private set; }

    public Guid? PurchaseOrderLineId { get; private set; }

    public Guid? MatchResultId { get; private set; }

    public Guid? MatchingRuleId { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    public static ExceptionCaseEvidence CreateInvoicePrice(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        Guid invoiceLineId,
        DateTimeOffset recordedAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId);
        if (invoiceLineId == Guid.Empty)
        {
            throw new ArgumentException("An invoice line identifier is required.", nameof(invoiceLineId));
        }

        return new ExceptionCaseEvidence(
            id,
            tenantId,
            exceptionCaseId,
            ExceptionEvidenceType.InvoicePrice,
            invoiceLineId,
            null,
            null,
            null,
            recordedAtUtc.ToUniversalTime());
    }

    public static ExceptionCaseEvidence CreatePurchaseOrderPrice(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        Guid purchaseOrderLineId,
        DateTimeOffset recordedAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId);
        if (purchaseOrderLineId == Guid.Empty)
        {
            throw new ArgumentException("A purchase order line identifier is required.", nameof(purchaseOrderLineId));
        }

        return new ExceptionCaseEvidence(
            id,
            tenantId,
            exceptionCaseId,
            ExceptionEvidenceType.PurchaseOrderPrice,
            null,
            purchaseOrderLineId,
            null,
            null,
            recordedAtUtc.ToUniversalTime());
    }

    public static ExceptionCaseEvidence CreatePriceVariance(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        Guid matchResultId,
        DateTimeOffset recordedAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId);
        if (matchResultId == Guid.Empty)
        {
            throw new ArgumentException("A match result identifier is required.", nameof(matchResultId));
        }

        return new ExceptionCaseEvidence(
            id,
            tenantId,
            exceptionCaseId,
            ExceptionEvidenceType.PriceVariance,
            null,
            null,
            matchResultId,
            null,
            recordedAtUtc.ToUniversalTime());
    }

    public static ExceptionCaseEvidence CreateRuleTolerance(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        Guid matchingRuleId,
        DateTimeOffset recordedAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId);
        if (matchingRuleId == Guid.Empty)
        {
            throw new ArgumentException("A matching rule identifier is required.", nameof(matchingRuleId));
        }

        return new ExceptionCaseEvidence(
            id,
            tenantId,
            exceptionCaseId,
            ExceptionEvidenceType.RuleTolerance,
            null,
            null,
            null,
            matchingRuleId,
            recordedAtUtc.ToUniversalTime());
    }

    private static void ValidateCommon(Guid id, Guid tenantId, Guid exceptionCaseId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An evidence identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        if (exceptionCaseId == Guid.Empty)
        {
            throw new ArgumentException("An exception case identifier is required.", nameof(exceptionCaseId));
        }
    }
}
