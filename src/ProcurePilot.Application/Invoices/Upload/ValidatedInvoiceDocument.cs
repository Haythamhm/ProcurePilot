namespace ProcurePilot.Application.Invoices.Upload;

public sealed record ValidatedInvoiceDocument(
    string FileName,
    string ContentType,
    string Extension,
    byte[] Content,
    string Sha256);
