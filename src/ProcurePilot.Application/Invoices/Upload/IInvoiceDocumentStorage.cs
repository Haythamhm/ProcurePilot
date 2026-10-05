namespace ProcurePilot.Application.Invoices.Upload;

public interface IInvoiceDocumentStorage
{
    Task StoreAsync(
        string storageReference,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string storageReference,
        CancellationToken cancellationToken = default);
}
