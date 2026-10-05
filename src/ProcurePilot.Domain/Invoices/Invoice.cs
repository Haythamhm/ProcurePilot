using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Invoices;

public sealed class Invoice : ITenantScopedEntity
{
    public const int NumberMaxLength = 100;
    public const int CurrencyMaxLength = 3;
    public const int ProcessingStatusMaxLength = 32;

    private readonly List<InvoiceLine> _lines = [];

    private Invoice()
    {
    }

    public Invoice(
        Guid id,
        Guid tenantId,
        Guid vendorId,
        string invoiceNumber,
        DateOnly invoiceDate,
        string currency,
        decimal totalAmount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An invoice identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        if (vendorId == Guid.Empty)
        {
            throw new ArgumentException("A vendor identifier is required.", nameof(vendorId));
        }

        if (invoiceDate == default)
        {
            throw new ArgumentException("An invoice date is required.", nameof(invoiceDate));
        }

        if (totalAmount < 0 || decimal.Round(totalAmount, 2) != totalAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount),
                "The total amount must be non-negative and have at most two decimal places.");
        }

        Id = id;
        TenantId = tenantId;
        VendorId = vendorId;
        InvoiceNumber = NormalizeRequired(invoiceNumber, NumberMaxLength, nameof(invoiceNumber));
        InvoiceDate = invoiceDate;
        Currency = NormalizeCurrency(currency);
        TotalAmount = totalAmount;
        ProcessingStatus = InvoiceProcessingStatus.Processing;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid VendorId { get; private set; }

    public string InvoiceNumber { get; private set; } = string.Empty;

    public DateOnly InvoiceDate { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public InvoiceProcessingStatus ProcessingStatus { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    public InvoiceLine AddLine(
        Guid id,
        int lineNumber,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        if (_lines.Any(line => line.LineNumber == lineNumber))
        {
            throw new InvalidOperationException($"Invoice line number {lineNumber} already exists.");
        }

        var line = new InvoiceLine(
            id,
            TenantId,
            Id,
            lineNumber,
            description,
            quantity,
            unitPrice);
        _lines.Add(line);
        return line;
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = NormalizeRequired(currency, CurrencyMaxLength, nameof(currency))
            .ToUpperInvariant();
        if (normalized.Length != CurrencyMaxLength || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException(
                "Currency must be a three-letter ISO code.",
                nameof(currency));
        }

        return normalized;
    }
}
