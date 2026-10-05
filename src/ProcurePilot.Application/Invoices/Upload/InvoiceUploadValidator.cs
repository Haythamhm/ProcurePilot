using System.Security.Cryptography;

namespace ProcurePilot.Application.Invoices.Upload;

public static class InvoiceUploadValidator
{
    private static readonly IReadOnlyDictionary<string, FileRule> FileRules =
        new Dictionary<string, FileRule>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new("application/pdf", [0x25, 0x50, 0x44, 0x46, 0x2D]),
            [".jpg"] = new("image/jpeg", [0xFF, 0xD8, 0xFF]),
            [".jpeg"] = new("image/jpeg", [0xFF, 0xD8, 0xFF]),
            [".png"] = new("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
        };

    public static async Task<ValidatedInvoiceDocument> ValidateAsync(
        InvoiceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var suppliedFileName = request.FileName?.Trim().Replace('\\', '/');
        var safeFileName = suppliedFileName?[(suppliedFileName.LastIndexOf('/') + 1)..];
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new InvoiceUploadValidationException("Select a PDF, JPEG, or PNG invoice document.");
        }

        if (request.SizeBytes is <= 0 or > InvoiceUploadLimits.MaxFileSizeBytes)
        {
            throw new InvoiceUploadValidationException("The document must be between 1 byte and 10 MB.");
        }

        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (!FileRules.TryGetValue(extension, out var rule) ||
            !string.Equals(request.ContentType, rule.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvoiceUploadValidationException("Only PDF, JPEG, and PNG documents are accepted.");
        }

        await using var content = new MemoryStream((int)request.SizeBytes);
        await request.Content.CopyToAsync(content, cancellationToken);
        if (content.Length != request.SizeBytes)
        {
            throw new InvoiceUploadValidationException("The uploaded document size could not be verified.");
        }

        var bytes = content.ToArray();
        if (!bytes.AsSpan().StartsWith(rule.Signature))
        {
            throw new InvoiceUploadValidationException("The document content does not match its file type.");
        }

        return new ValidatedInvoiceDocument(
            safeFileName,
            rule.ContentType,
            extension,
            bytes,
            Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    private sealed record FileRule(string ContentType, byte[] Signature);
}
