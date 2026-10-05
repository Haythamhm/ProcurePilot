namespace ProcurePilot.Application.Invoices.Upload;

public sealed record InvoiceUploadResult(
    Guid InvoiceId,
    Guid DocumentId,
    string FileName,
    long SizeBytes);
