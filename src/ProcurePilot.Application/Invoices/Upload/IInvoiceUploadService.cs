namespace ProcurePilot.Application.Invoices.Upload;

public interface IInvoiceUploadService
{
    Task<InvoiceUploadResult> UploadAsync(
        InvoiceUploadRequest request,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default);
}
