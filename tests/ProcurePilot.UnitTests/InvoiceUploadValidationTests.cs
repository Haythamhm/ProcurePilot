using System.Text;
using ProcurePilot.Application.Invoices.Upload;

namespace ProcurePilot.UnitTests;

public sealed class InvoiceUploadValidationTests
{
    [Fact]
    public async Task ValidPdfIsNormalizedAndHasDeterministicHash()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\ninvoice");
        await using var content = new MemoryStream(bytes);

        var result = await InvoiceUploadValidator.ValidateAsync(
            new InvoiceUploadRequest(content, " C:\\unsafe\\invoice.PDF ", "application/pdf", bytes.Length));

        Assert.Equal("invoice.PDF", result.FileName);
        Assert.Equal(".pdf", result.Extension);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(64, result.Sha256.Length);
    }

    [Fact]
    public async Task SpoofedPdfIsRejectedBySignature()
    {
        var bytes = Encoding.ASCII.GetBytes("not a pdf");
        await using var content = new MemoryStream(bytes);

        var error = await Assert.ThrowsAsync<InvoiceUploadValidationException>(() =>
            InvoiceUploadValidator.ValidateAsync(
                new InvoiceUploadRequest(content, "invoice.pdf", "application/pdf", bytes.Length)));

        Assert.Contains("does not match", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("invoice.exe", "application/octet-stream")]
    [InlineData("invoice.pdf", "image/png")]
    public async Task UnsupportedOrMismatchedTypeIsRejected(string fileName, string contentType)
    {
        await using var content = new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D]);

        await Assert.ThrowsAsync<InvoiceUploadValidationException>(() =>
            InvoiceUploadValidator.ValidateAsync(
                new InvoiceUploadRequest(content, fileName, contentType, content.Length)));
    }
}
