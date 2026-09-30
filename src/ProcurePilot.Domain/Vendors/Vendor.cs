using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Vendors;

public sealed class Vendor : ITenantScopedEntity
{
    public const int ExternalReferenceMaxLength = 100;
    public const int NameMaxLength = 200;
    public const int TaxIdentifierMaxLength = 100;

    private Vendor()
    {
    }

    public Vendor(
        Guid id,
        Guid tenantId,
        string externalReference,
        string name,
        string? taxIdentifier = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A vendor identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        Id = id;
        TenantId = tenantId;
        ExternalReference = NormalizeRequired(
            externalReference,
            ExternalReferenceMaxLength,
            nameof(externalReference));
        Name = NormalizeRequired(name, NameMaxLength, nameof(name));
        TaxIdentifier = NormalizeOptional(
            taxIdentifier,
            TaxIdentifierMaxLength,
            nameof(taxIdentifier));
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string ExternalReference { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? TaxIdentifier { get; private set; }

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

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
