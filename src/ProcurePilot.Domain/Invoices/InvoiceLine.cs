using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Invoices;

public sealed class InvoiceLine : ITenantScopedEntity
{
    public const int DescriptionMaxLength = 500;

    private InvoiceLine()
    {
    }

    internal InvoiceLine(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        int lineNumber,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An invoice line identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        if (invoiceId == Guid.Empty)
        {
            throw new ArgumentException("An invoice identifier is required.", nameof(invoiceId));
        }

        if (lineNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineNumber),
                "A line number must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        var normalizedDescription = description.Trim();
        if (normalizedDescription.Length > DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"The description cannot exceed {DescriptionMaxLength} characters.",
                nameof(description));
        }

        if (quantity <= 0 || decimal.Round(quantity, 4) != quantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be positive and have at most four decimal places.");
        }

        if (unitPrice < 0 || decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price must be non-negative and have at most two decimal places.");
        }

        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        LineNumber = lineNumber;
        Description = normalizedDescription;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public int LineNumber { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }
}
