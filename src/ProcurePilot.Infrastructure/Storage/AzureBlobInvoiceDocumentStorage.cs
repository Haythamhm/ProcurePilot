using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProcurePilot.Application.Invoices.Upload;

namespace ProcurePilot.Infrastructure.Storage;

public sealed class AzureBlobInvoiceDocumentStorage(BlobServiceClient blobServiceClient)
    : IInvoiceDocumentStorage
{
    private const string ContainerName = "invoice-documents";

    public async Task StoreAsync(
        string storageReference,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(storageReference);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);
    }

    public async Task DeleteIfExistsAsync(
        string storageReference,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        await container.DeleteBlobIfExistsAsync(
            storageReference,
            cancellationToken: cancellationToken);
    }
}
