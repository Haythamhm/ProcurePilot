using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Purchasing;

public sealed class PurchaseOrder : ITenantScopedEntity
{
    public const int NumberMaxLength = 50;
    public const int CurrencyMaxLength = 3;
    public const int StatusMaxLength = 32;

    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
    }

    public PurchaseOrder(
        Guid id,
        Guid tenantId,
        Guid vendorId,
        string number,
        DateOnly orderDate,
        string currency,
        string status,
        decimal totalAmount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A purchase order identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        if (vendorId == Guid.Empty)
        {
            throw new ArgumentException("A vendor identifier is required.", nameof(vendorId));
        }

        if (orderDate == default)
        {
            throw new ArgumentException("An order date is required.", nameof(orderDate));
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
        Number = NormalizeRequired(number, NumberMaxLength, nameof(number));
        OrderDate = orderDate;
        Currency = NormalizeCurrency(currency);
        Status = NormalizeRequired(status, StatusMaxLength, nameof(status));
        TotalAmount = totalAmount;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid VendorId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public DateOnly OrderDate { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public PurchaseOrderLine AddLine(
        Guid id,
        int lineNumber,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        if (_lines.Any(line => line.LineNumber == lineNumber))
        {
            throw new InvalidOperationException(
                $"Purchase order line number {lineNumber} already exists.");
        }

        var line = new PurchaseOrderLine(
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
