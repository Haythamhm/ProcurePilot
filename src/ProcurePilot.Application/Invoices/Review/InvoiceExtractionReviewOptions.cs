namespace ProcurePilot.Application.Invoices.Review;

public sealed class InvoiceExtractionReviewOptions
{
    public const string SectionName = "InvoiceExtractionReview";

    public double ConfidenceThreshold { get; set; }

    public string[] CriticalFields { get; set; } = [];

    public void Validate()
    {
        if (ConfidenceThreshold is <= 0 or > 1)
        {
            throw new InvalidOperationException(
                "Invoice extraction confidence threshold must be greater than zero and at most one.");
        }

        if (CriticalFields.Length == 0)
        {
            throw new InvalidOperationException("At least one critical extraction field is required.");
        }

        var allowedFields = Enum.GetNames<InvoiceExtractionField>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalidFields = CriticalFields
            .Where(field => !allowedFields.Contains(field))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (invalidFields.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unknown critical extraction fields: {string.Join(", ", invalidFields)}.");
        }
    }
}

public enum InvoiceExtractionField
{
    InvoiceNumber,
    SupplierName,
    PurchaseOrderNumber,
    InvoiceDate,
    Currency,
    TotalAmount
}
