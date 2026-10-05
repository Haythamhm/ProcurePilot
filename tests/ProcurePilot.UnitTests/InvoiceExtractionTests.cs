using Microsoft.Extensions.AI;
using ProcurePilot.Application.Invoices.Extraction;

namespace ProcurePilot.UnitTests;

public sealed class InvoiceExtractionTests
{
    [Fact]
    public async Task ExtractAsyncUsesStructuredProviderNeutralChatRequest()
    {
        const string response = """
            {
              "invoiceNumber": { "value": "INV-42", "confidence": 0.99 },
              "supplierName": { "value": "Atlas Supply", "confidence": 0.97 },
              "purchaseOrderNumber": { "value": "PO-12", "confidence": 0.96 },
              "invoiceDate": { "value": "2026-10-05", "confidence": 0.95 },
              "currency": { "value": "USD", "confidence": 0.99 },
              "subtotalAmount": { "value": 100.00, "confidence": 0.98 },
              "taxAmount": { "value": 20.00, "confidence": 0.98 },
              "totalAmount": { "value": 120.00, "confidence": 0.99 },
              "lineItems": [
                {
                  "lineNumber": 1,
                  "description": { "value": "Control module", "confidence": 0.94 },
                  "quantity": { "value": 1, "confidence": 0.98 },
                  "unitPrice": { "value": 100.00, "confidence": 0.97 },
                  "lineTotal": { "value": 100.00, "confidence": 0.97 }
                }
              ]
            }
            """;
        using var chatClient = new StubChatClient(response);
        var extractor = new ChatClientInvoiceExtractor(chatClient);

        var result = await extractor.ExtractAsync(
            new InvoiceExtractionRequest(new byte[] { 1, 2, 3 }, "application/pdf"));

        Assert.Equal("INV-42", result.InvoiceNumber.Value);
        Assert.Equal("Atlas Supply", result.SupplierName.Value);
        Assert.Equal(120.00m, result.TotalAmount.Value);
        Assert.Single(result.LineItems);
        Assert.Contains("untrusted", chatClient.Messages[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(chatClient.Messages[1].Contents, content => content is DataContent);
        var responseFormat = Assert.IsType<ChatResponseFormatJson>(
            chatClient.Options.ResponseFormat);
        Assert.NotNull(responseFormat.Schema);
    }

    [Fact]
    public async Task ExtractAsyncRejectsUnsupportedMediaBeforeCallingProvider()
    {
        using var chatClient = new StubChatClient("{}");
        var extractor = new ChatClientInvoiceExtractor(chatClient);

        await Assert.ThrowsAsync<ArgumentException>(() => extractor.ExtractAsync(
            new InvoiceExtractionRequest(new byte[] { 1 }, "text/plain")));

        Assert.Empty(chatClient.Messages);
    }

    [Fact]
    public async Task ExtractAsyncRejectsMalformedProviderResponse()
    {
        using var chatClient = new StubChatClient("not-json");
        var extractor = new ChatClientInvoiceExtractor(chatClient);

        var error = await Assert.ThrowsAsync<InvoiceExtractionException>(() =>
            extractor.ExtractAsync(new InvoiceExtractionRequest(new byte[] { 1 }, "image/png")));

        Assert.Contains("invalid structured data", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubChatClient(string responseText) : IChatClient
    {
        public IReadOnlyList<ChatMessage> Messages { get; private set; } = [];

        public ChatOptions Options { get; private set; } = new();

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Messages = messages.ToList();
            Options = options ?? new ChatOptions();
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, responseText)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
