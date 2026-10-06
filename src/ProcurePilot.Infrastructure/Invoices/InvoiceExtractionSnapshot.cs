using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Infrastructure.Invoices;

public sealed class InvoiceExtractionSnapshot : ITenantScopedEntity
{
    public const int PayloadMaxLength = 100_000;

    private InvoiceExtractionSnapshot()
    {
    }

    public InvoiceExtractionSnapshot(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        string originalPayload,
        DateTimeOffset extractedAtUtc)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || invoiceId == Guid.Empty)
        {
            throw new ArgumentException("Extraction, tenant, and invoice identifiers are required.");
        }

        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        OriginalPayload = ValidatePayload(originalPayload, nameof(originalPayload));
        ExtractedAtUtc = extractedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public string OriginalPayload { get; private set; } = string.Empty;

    public DateTimeOffset ExtractedAtUtc { get; private set; }

    public string? ReviewedPayload { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public void ReplaceOriginal(string payload, DateTimeOffset extractedAtUtc)
    {
        if (ReviewedAtUtc.HasValue)
        {
            throw new InvalidOperationException("A confirmed extraction cannot be replaced.");
        }

        OriginalPayload = ValidatePayload(payload, nameof(payload));
        ExtractedAtUtc = extractedAtUtc;
    }

    public void MarkReviewed(string payload, Guid reviewedByUserId, DateTimeOffset reviewedAtUtc)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            throw new ArgumentException("A reviewer identifier is required.", nameof(reviewedByUserId));
        }

        if (ReviewedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The extraction has already been reviewed.");
        }

        ReviewedPayload = ValidatePayload(payload, nameof(payload));
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = reviewedAtUtc;
    }

    private static string ValidatePayload(string payload, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload, parameterName);
        if (payload.Length > PayloadMaxLength)
        {
            throw new ArgumentException(
                $"Extraction payload cannot exceed {PayloadMaxLength} characters.",
                parameterName);
        }

        return payload;
    }
}
