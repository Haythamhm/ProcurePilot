namespace ProcurePilot.Application.Invoices.Extraction;

public sealed record InvoiceExtractionResult
{
    public required ExtractedField<string?> InvoiceNumber { get; init; }

    public required ExtractedField<string?> SupplierName { get; init; }

    public required ExtractedField<string?> PurchaseOrderNumber { get; init; }

    public required ExtractedField<DateOnly?> InvoiceDate { get; init; }

    public required ExtractedField<string?> Currency { get; init; }

    public required ExtractedField<decimal?> SubtotalAmount { get; init; }

    public required ExtractedField<decimal?> TaxAmount { get; init; }

    public required ExtractedField<decimal?> TotalAmount { get; init; }

    public required IReadOnlyList<InvoiceLineExtraction> LineItems { get; init; }
}

public sealed record InvoiceLineExtraction
{
    public required int LineNumber { get; init; }

    public required ExtractedField<string?> Description { get; init; }

    public required ExtractedField<decimal?> Quantity { get; init; }

    public required ExtractedField<decimal?> UnitPrice { get; init; }

    public required ExtractedField<decimal?> LineTotal { get; init; }
}

public sealed record ExtractedField<T>
{
    public required T Value { get; init; }

    public required double Confidence { get; init; }
}
