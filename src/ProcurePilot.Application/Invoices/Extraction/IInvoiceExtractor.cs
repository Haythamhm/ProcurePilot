namespace ProcurePilot.Application.Invoices.Extraction;

public interface IInvoiceExtractor
{
    Task<InvoiceExtractionResult> ExtractAsync(
        InvoiceExtractionRequest request,
        CancellationToken cancellationToken = default);
}
