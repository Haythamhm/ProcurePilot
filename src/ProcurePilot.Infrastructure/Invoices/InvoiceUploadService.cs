using ProcurePilot.Application.Invoices.Upload;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Invoices;
using ProcurePilot.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ProcurePilot.Infrastructure.Invoices;

public sealed class InvoiceUploadService(
    ProcurePilotDbContext dbContext,
    ITenantContext tenantContext,
    IInvoiceDocumentStorage documentStorage,
    TimeProvider timeProvider,
    ILogger<InvoiceUploadService> logger) : IInvoiceUploadService
{
    public async Task<InvoiceUploadResult> UploadAsync(
        InvoiceUploadRequest request,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("An uploader identifier is required.", nameof(uploadedByUserId));
        }

        var tenantId = tenantContext.RequireTenantId();
        var validated = await InvoiceUploadValidator.ValidateAsync(request, cancellationToken);
        var invoiceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var storageReference = $"{tenantId:N}/{invoiceId:N}/{documentId:N}{validated.Extension}";

        await using var bufferedContent = new MemoryStream(validated.Content, writable: false);
        await documentStorage.StoreAsync(
            storageReference,
            bufferedContent,
            validated.ContentType,
            cancellationToken);

        try
        {
            var invoice = Invoice.CreateProcessing(invoiceId, tenantId);
            var document = new InvoiceDocument(
                documentId,
                tenantId,
                invoiceId,
                uploadedByUserId,
                validated.FileName,
                validated.ContentType,
                validated.Content.LongLength,
                storageReference,
                validated.Sha256,
                timeProvider.GetUtcNow());

            dbContext.Invoices.Add(invoice);
            dbContext.InvoiceDocuments.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new InvoiceUploadResult(
                invoiceId,
                documentId,
                validated.FileName,
                validated.Content.LongLength);
        }
        catch (Exception exception)
        {
            try
            {
                await documentStorage.DeleteIfExistsAsync(storageReference, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Failed to remove orphaned invoice blob {StorageReference} after persistence failure.",
                    storageReference);
            }

            logger.LogError(
                exception,
                "Invoice metadata persistence failed for blob {StorageReference}.",
                storageReference);
            throw;
        }
    }
}
