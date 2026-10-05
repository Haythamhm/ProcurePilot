namespace ProcurePilot.Application.Invoices.Extraction;

public sealed record InvoiceExtractionRequest(
    ReadOnlyMemory<byte> Content,
    string MediaType);
