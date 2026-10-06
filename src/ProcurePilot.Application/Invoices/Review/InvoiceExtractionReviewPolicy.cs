using ProcurePilot.Application.Invoices.Extraction;

namespace ProcurePilot.Application.Invoices.Review;

public static class InvoiceExtractionReviewPolicy
{
    public static InvoiceExtractionAssessment Assess(
        InvoiceExtractionResult extraction,
        InvoiceExtractionReviewOptions options)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var issues = new List<InvoiceExtractionIssue>();
        foreach (var configuredField in options.CriticalFields)
        {
            var field = Enum.Parse<InvoiceExtractionField>(configuredField, ignoreCase: true);
            var (hasValue, confidence) = GetValueState(extraction, field);
            if (!hasValue || confidence < options.ConfidenceThreshold)
            {
                issues.Add(new InvoiceExtractionIssue(field, confidence, !hasValue));
            }
        }

        return new InvoiceExtractionAssessment(options.ConfidenceThreshold, issues);
    }

    private static (bool HasValue, double Confidence) GetValueState(
        InvoiceExtractionResult extraction,
        InvoiceExtractionField field) => field switch
        {
            InvoiceExtractionField.InvoiceNumber =>
                (!string.IsNullOrWhiteSpace(extraction.InvoiceNumber.Value), extraction.InvoiceNumber.Confidence),
            InvoiceExtractionField.SupplierName =>
                (!string.IsNullOrWhiteSpace(extraction.SupplierName.Value), extraction.SupplierName.Confidence),
            InvoiceExtractionField.PurchaseOrderNumber =>
                (!string.IsNullOrWhiteSpace(extraction.PurchaseOrderNumber.Value), extraction.PurchaseOrderNumber.Confidence),
            InvoiceExtractionField.InvoiceDate =>
                (extraction.InvoiceDate.Value.HasValue, extraction.InvoiceDate.Confidence),
            InvoiceExtractionField.Currency =>
                (!string.IsNullOrWhiteSpace(extraction.Currency.Value), extraction.Currency.Confidence),
            InvoiceExtractionField.TotalAmount =>
                (extraction.TotalAmount.Value.HasValue, extraction.TotalAmount.Confidence),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unsupported extraction field.")
        };
}

public sealed record InvoiceExtractionAssessment(
    double ConfidenceThreshold,
    IReadOnlyList<InvoiceExtractionIssue> Issues)
{
    public bool RequiresConfirmation => Issues.Count > 0;
}

public sealed record InvoiceExtractionIssue(
    InvoiceExtractionField Field,
    double Confidence,
    bool IsMissing);
