namespace ProcurePilot.Application.Invoices.Upload;

public sealed record InvoiceUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);
