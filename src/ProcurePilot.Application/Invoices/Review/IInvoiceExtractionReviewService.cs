using ProcurePilot.Application.Invoices.Extraction;

namespace ProcurePilot.Application.Invoices.Review;

public interface IInvoiceExtractionReviewService
{
    Task<InvoiceReviewReadModel?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task SaveExtractionAsync(
        Guid invoiceId,
        InvoiceExtractionResult extraction,
        CancellationToken cancellationToken = default);

    Task ConfirmAsync(
        ConfirmInvoiceExtractionCommand command,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default);
}

public sealed record InvoiceReviewReadModel(
    Guid InvoiceId,
    string OriginalFileName,
    string ProcessingStatus,
    InvoiceExtractionResult Extraction,
    InvoiceExtractionAssessment Assessment,
    IReadOnlyList<InvoiceReviewVendor> Vendors,
    bool IsConfirmed);

public sealed record InvoiceReviewVendor(Guid Id, string Name);

public sealed record ConfirmInvoiceExtractionCommand(
    Guid InvoiceId,
    Guid VendorId,
    string InvoiceNumber,
    string? PurchaseOrderNumber,
    DateOnly InvoiceDate,
    string Currency,
    decimal? SubtotalAmount,
    decimal? TaxAmount,
    decimal TotalAmount,
    IReadOnlyList<ConfirmInvoiceLineCommand> Lines,
    bool ConfirmLowConfidenceFields);

public sealed record ConfirmInvoiceLineCommand(
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public sealed class InvoiceReviewValidationException(string message) : Exception(message);
