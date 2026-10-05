namespace ProcurePilot.Application.Invoices.Extraction;

public sealed class InvoiceExtractionException : Exception
{
    public InvoiceExtractionException(string message)
        : base(message)
    {
    }

    public InvoiceExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
