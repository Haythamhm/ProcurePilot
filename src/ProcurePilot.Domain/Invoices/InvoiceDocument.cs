using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Invoices;

public sealed class InvoiceDocument : ITenantScopedEntity
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 100;
    public const int StorageReferenceMaxLength = 500;
    public const int Sha256MaxLength = 64;

    private InvoiceDocument()
    {
    }

    public InvoiceDocument(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        Guid uploadedByUserId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageReference,
        string sha256,
        DateTimeOffset uploadedAtUtc)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || invoiceId == Guid.Empty || uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Document, tenant, invoice, and uploader identifiers are required.");
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size must be positive.");
        }

        if (uploadedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Upload timestamp must be UTC.", nameof(uploadedAtUtc));
        }

        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        UploadedByUserId = uploadedByUserId;
        OriginalFileName = Normalize(originalFileName, FileNameMaxLength, nameof(originalFileName));
        ContentType = Normalize(contentType, ContentTypeMaxLength, nameof(contentType)).ToLowerInvariant();
        SizeBytes = sizeBytes;
        StorageReference = Normalize(storageReference, StorageReferenceMaxLength, nameof(storageReference));
        Sha256 = Normalize(sha256, Sha256MaxLength, nameof(sha256)).ToLowerInvariant();
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageReference { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; private set; }

    private static string Normalize(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"The value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }
}
