namespace ProcurePilot.Application.Invoices.Upload;

public sealed class InvoiceUploadValidationException(string message) : Exception(message);
