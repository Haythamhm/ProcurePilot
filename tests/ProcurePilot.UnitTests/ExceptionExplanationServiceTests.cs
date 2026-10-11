using Microsoft.Extensions.AI;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Application.Exceptions.Explanation;

namespace ProcurePilot.UnitTests;

public sealed class ExceptionExplanationServiceTests
{
    private static readonly Guid SampleTenantId = Guid.NewGuid();
    private static readonly Guid SampleCaseId = Guid.NewGuid();

    private static readonly ExceptionExplanationRequest SampleRequest = new(
        SampleTenantId,
        SampleCaseId,
        VendorName: "Acme Industrial",
        InvoiceNumber: "INV-2026-901",
        InvoiceUnitPrice: 125.00m,
        InvoiceCurrency: "EUR",
        PurchaseOrderNumber: "PO-4401",
        PurchaseOrderUnitPrice: 100.00m,
        PurchaseOrderCurrency: "EUR",
        VariancePercent: 25.00m,
        AllowedTolerancePercent: 5.00m,
        RuleType: "PriceTolerance",
        Evidences:
        [
            new ExceptionWorkspaceEvidenceReadModel(
                Type: "UnitPriceVariance",
                Label: "Price discrepancy",
                SourceReference: "PO-4401:1 -> INV-2026-901:1",
                ValueText: "+25.00% variance exceeding +5.00% tolerance",
                RecordedAtUtc: DateTimeOffset.UtcNow)
        ]);

    [Fact]
    public async Task ExplainAsync_ProducesStructuredSeparatedDomains_WithMetadataAndEvidenceReferences()
    {
        const string mockJson = """
            {
              "facts": {
                "vendorName": "Acme Industrial",
                "invoiceNumber": "INV-2026-901",
                "invoiceUnitPrice": 125.00,
                "invoiceCurrency": "EUR",
                "purchaseOrderNumber": "PO-4401",
                "purchaseOrderUnitPrice": 100.00,
                "purchaseOrderCurrency": "EUR",
                "variancePercent": 25.00,
                "allowedTolerancePercent": 5.00,
                "ruleType": "PriceTolerance"
              },
              "interpretation": {
                "summary": "Invoice unit price exceeds PO rate by 25.00%, surpassing the 5.00% allowed tolerance.",
                "rootCauseCategory": "Unit Price Mismatch",
                "contributingFactors": [
                  "Supplier billed 125.00 EUR per unit instead of agreed 100.00 EUR",
                  "No price adjustment addendum found on the PO"
                ]
              },
              "recommendation": {
                "primaryAction": "Request a credit note of 25.00 EUR per unit or obtain buyer variance approval.",
                "suggestedSteps": [
                  "Contact Acme Industrial billing department regarding invoice INV-2026-901",
                  "Verify whether a renegotiated rate schedule was approved outside the PO"
                ],
                "riskAssessment": "Low risk of operational disruption; high financial variance requiring dispute or authorization."
              },
              "evidenceReferences": [
                {
                  "evidenceType": "UnitPriceVariance",
                  "sourceReference": "PO-4401:1 -> INV-2026-901:1",
                  "description": "+25.00% variance exceeding +5.00% tolerance"
                }
              ],
              "promptVersion": "outdated-version",
              "modelName": "model-from-json",
              "confidenceScore": 0.94,
              "generatedAtUtc": "2026-01-01T00:00:00Z"
            }
            """;

        using var chatClient = new StubChatClient(mockJson, modelId: "gpt-4o-mini");
        var service = new ChatClientExceptionExplanationService(chatClient);

        var result = await service.ExplainAsync(SampleRequest);

        // Deterministic facts separation
        Assert.NotNull(result.Facts);
        Assert.Equal("Acme Industrial", result.Facts.VendorName);
        Assert.Equal("INV-2026-901", result.Facts.InvoiceNumber);
        Assert.Equal(125.00m, result.Facts.InvoiceUnitPrice);
        Assert.Equal(25.00m, result.Facts.VariancePercent);

        // Interpretation separation
        Assert.NotNull(result.Interpretation);
        Assert.Contains("25.00%", result.Interpretation.Summary);
        Assert.Equal("Unit Price Mismatch", result.Interpretation.RootCauseCategory);
        Assert.Equal(2, result.Interpretation.ContributingFactors.Count);

        // Recommendation separation
        Assert.NotNull(result.Recommendation);
        Assert.Contains("credit note", result.Recommendation.PrimaryAction, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, result.Recommendation.SuggestedSteps.Count);
        Assert.NotEmpty(result.Recommendation.RiskAssessment);

        // Traceability and metadata
        Assert.Single(result.EvidenceReferences);
        Assert.Equal("UnitPriceVariance", result.EvidenceReferences[0].EvidenceType);
        Assert.Equal("PO-4401:1 -> INV-2026-901:1", result.EvidenceReferences[0].SourceReference);
        Assert.Equal(ExceptionExplanationPrompt.CurrentVersion, result.PromptVersion);
        Assert.Equal("gpt-4o-mini", result.ModelName);
        Assert.Equal(0.94m, result.ConfidenceScore);
        Assert.True(result.GeneratedAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.NotNull(result.Telemetry);
        Assert.True(result.Telemetry.DurationMs >= 0);
        Assert.Equal(mockJson, result.RawPayloadJson);

        // Provider neutral options and chat request inspection
        Assert.Equal(2, chatClient.Messages.Count);
        Assert.Equal(ChatRole.System, chatClient.Messages[0].Role);
        Assert.Equal(ChatRole.User, chatClient.Messages[1].Role);
        var jsonFormat = Assert.IsType<ChatResponseFormatJson>(chatClient.Options.ResponseFormat);
        Assert.NotNull(jsonFormat.Schema);
        Assert.Equal("exception_explanation", jsonFormat.SchemaName);
    }

    [Fact]
    public void SystemPrompt_EnforcesNonDecisionPolicy_UntrustedSupplierData_AndSeparatedDomains()
    {
        var prompt = ExceptionExplanationPrompt.SystemPrompt;

        Assert.Contains("DO NOT make financial decisions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO NOT recalculate", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("strictly separate output into three independent domains", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Facts", prompt);
        Assert.Contains("Interpretation", prompt);
        Assert.Contains("Recommendation", prompt);
        Assert.Contains("untrusted data", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("confidence score between 0.0 and 1.0", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUserPrompt_IncludesDeterministicBaselineAndEvidences()
    {
        var userPrompt = ExceptionExplanationPrompt.BuildUserPrompt(SampleRequest);

        Assert.Contains("Acme Industrial", userPrompt);
        Assert.Contains("INV-2026-901", userPrompt);
        Assert.Contains("125.00 EUR", userPrompt);
        Assert.Contains("PO-4401", userPrompt);
        Assert.Contains("100.00 EUR", userPrompt);
        Assert.Contains("25.00%", userPrompt);
        Assert.Contains("5.00%", userPrompt);
        Assert.Contains("PriceTolerance", userPrompt);
        Assert.Contains("PO-4401:1 -> INV-2026-901:1", userPrompt);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "e7a33077-8374-4b5c-b179-873b5cf2d9c1", "Vendor", "INV-1", "PO-1")]
    [InlineData("e7a33077-8374-4b5c-b179-873b5cf2d9c1", "00000000-0000-0000-0000-000000000000", "Vendor", "INV-1", "PO-1")]
    [InlineData("e7a33077-8374-4b5c-b179-873b5cf2d9c1", "e7a33077-8374-4b5c-b179-873b5cf2d9c1", "", "INV-1", "PO-1")]
    [InlineData("e7a33077-8374-4b5c-b179-873b5cf2d9c1", "e7a33077-8374-4b5c-b179-873b5cf2d9c1", "Vendor", "", "PO-1")]
    [InlineData("e7a33077-8374-4b5c-b179-873b5cf2d9c1", "e7a33077-8374-4b5c-b179-873b5cf2d9c1", "Vendor", "INV-1", "")]
    public async Task ExplainAsync_ThrowsArgumentException_WhenRequiredInputIsMissing(
        string tenantIdStr,
        string caseIdStr,
        string vendor,
        string invoiceNo,
        string poNo)
    {
        using var chatClient = new StubChatClient("{}");
        var service = new ChatClientExceptionExplanationService(chatClient);

        var request = new ExceptionExplanationRequest(
            Guid.Parse(tenantIdStr),
            Guid.Parse(caseIdStr),
            vendor,
            invoiceNo,
            100m,
            "EUR",
            poNo,
            80m,
            "EUR",
            25m,
            5m,
            "Tolerance",
            []);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ExplainAsync(request));
    }

    [Fact]
    public async Task ExplainAsync_ThrowsUnavailableException_WhenProviderIsDisabled()
    {
        using var chatClient = new ThrowingChatClient(
            new InvalidOperationException("The AI chat provider is disabled in application configuration."));
        var service = new ChatClientExceptionExplanationService(chatClient);

        var ex = await Assert.ThrowsAsync<ExceptionExplanationUnavailableException>(() =>
            service.ExplainAsync(SampleRequest));

        Assert.Contains("disabled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplainAsync_ThrowsUnavailableException_WhenProviderFails()
    {
        using var chatClient = new ThrowingChatClient(
            new HttpRequestException("Connection refused by upstream AI endpoint."));
        var service = new ChatClientExceptionExplanationService(chatClient);

        var ex = await Assert.ThrowsAsync<ExceptionExplanationUnavailableException>(() =>
            service.ExplainAsync(SampleRequest));

        Assert.Contains("unavailable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not a valid json string")]
    public async Task ExplainAsync_ThrowsException_WhenResponseIsEmptyOrInvalidJson(string response)
    {
        using var chatClient = new StubChatClient(response);
        var service = new ChatClientExceptionExplanationService(chatClient);

        await Assert.ThrowsAsync<ExceptionExplanationException>(() =>
            service.ExplainAsync(SampleRequest));
    }

    [Fact]
    public async Task ExplainAsync_ThrowsException_WhenFactsAreMissing()
    {
        const string json = """
            {
              "interpretation": { "summary": "test", "rootCauseCategory": "test", "contributingFactors": [] },
              "recommendation": { "primaryAction": "test", "suggestedSteps": [], "riskAssessment": "test" },
              "confidenceScore": 0.8
            }
            """;
        using var chatClient = new StubChatClient(json);
        var service = new ChatClientExceptionExplanationService(chatClient);

        var ex = await Assert.ThrowsAsync<ExceptionExplanationException>(() =>
            service.ExplainAsync(SampleRequest));

        Assert.Contains("deterministic facts", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplainAsync_ThrowsException_WhenInterpretationIsMissing()
    {
        const string json = """
            {
              "facts": { "vendorName": "V", "invoiceNumber": "I", "invoiceUnitPrice": 1, "invoiceCurrency": "EUR", "purchaseOrderNumber": "P", "purchaseOrderUnitPrice": 1, "purchaseOrderCurrency": "EUR", "variancePercent": 0, "allowedTolerancePercent": 0, "ruleType": "R" },
              "recommendation": { "primaryAction": "test", "suggestedSteps": [], "riskAssessment": "test" },
              "confidenceScore": 0.8
            }
            """;
        using var chatClient = new StubChatClient(json);
        var service = new ChatClientExceptionExplanationService(chatClient);

        var ex = await Assert.ThrowsAsync<ExceptionExplanationException>(() =>
            service.ExplainAsync(SampleRequest));

        Assert.Contains("interpretation summary", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplainAsync_ThrowsException_WhenRecommendationIsMissing()
    {
        const string json = """
            {
              "facts": { "vendorName": "V", "invoiceNumber": "I", "invoiceUnitPrice": 1, "invoiceCurrency": "EUR", "purchaseOrderNumber": "P", "purchaseOrderUnitPrice": 1, "purchaseOrderCurrency": "EUR", "variancePercent": 0, "allowedTolerancePercent": 0, "ruleType": "R" },
              "interpretation": { "summary": "test", "rootCauseCategory": "test", "contributingFactors": [] },
              "confidenceScore": 0.8
            }
            """;
        using var chatClient = new StubChatClient(json);
        var service = new ChatClientExceptionExplanationService(chatClient);

        var ex = await Assert.ThrowsAsync<ExceptionExplanationException>(() =>
            service.ExplainAsync(SampleRequest));

        Assert.Contains("primary recommendation action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.05)]
    public async Task ExplainAsync_ThrowsException_WhenConfidenceScoreIsOutOfRange(decimal score)
    {
        var scoreString = score.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var json = $$"""
            {
              "facts": { "vendorName": "V", "invoiceNumber": "I", "invoiceUnitPrice": 1, "invoiceCurrency": "EUR", "purchaseOrderNumber": "P", "purchaseOrderUnitPrice": 1, "purchaseOrderCurrency": "EUR", "variancePercent": 0, "allowedTolerancePercent": 0, "ruleType": "R" },
              "interpretation": { "summary": "test", "rootCauseCategory": "test", "contributingFactors": [] },
              "recommendation": { "primaryAction": "test", "suggestedSteps": [], "riskAssessment": "test" },
              "confidenceScore": {{scoreString}}
            }
            """;
        using var chatClient = new StubChatClient(json);
        var service = new ChatClientExceptionExplanationService(chatClient);

        var ex = await Assert.ThrowsAsync<ExceptionExplanationException>(() =>
            service.ExplainAsync(SampleRequest));

        Assert.Contains("confidence score must be between 0.0 and 1.0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromWorkspaceModel_MapsAllFactualFieldsAndEvidences()
    {
        var workspaceModel = new ExceptionWorkspaceReadModel(
            Id: SampleCaseId,
            Type: "PriceTolerance",
            Status: "Open",
            Severity: "High",
            OpenedAtUtc: DateTimeOffset.UtcNow,
            ResolvedAtUtc: null,
            AssignedToDisplayName: "Jane Buyer",
            VendorName: "Acme Industrial",
            InvoiceNumber: "INV-2026-901",
            InvoiceCurrency: "EUR",
            InvoiceLineNumber: 1,
            InvoiceLineDescription: "Hydraulic Pump",
            InvoiceQuantity: 10,
            InvoiceUnitPrice: 125.00m,
            InvoiceSourceFileName: "inv-901.pdf",
            PurchaseOrderNumber: "PO-4401",
            PurchaseOrderCurrency: "EUR",
            PurchaseOrderLineNumber: 1,
            PurchaseOrderLineDescription: "Hydraulic Pump",
            PurchaseOrderQuantity: 10,
            PurchaseOrderUnitPrice: 100.00m,
            VariancePercent: 25.00m,
            AllowedTolerancePercent: 5.00m,
            MatchingRuleType: "PriceTolerance",
            MatchingRuleVersion: 1,
            EvaluatedAtUtc: DateTimeOffset.UtcNow,
            Evidences:
            [
                new ExceptionWorkspaceEvidenceReadModel(
                    Type: "UnitPriceVariance",
                    Label: "Price discrepancy",
                    SourceReference: "PO-4401:1 -> INV-2026-901:1",
                    ValueText: "+25.00%",
                    RecordedAtUtc: DateTimeOffset.UtcNow)
            ]);

        var request = ExceptionExplanationRequest.FromWorkspaceModel(SampleTenantId, workspaceModel);

        Assert.Equal(SampleTenantId, request.TenantId);
        Assert.Equal(SampleCaseId, request.ExceptionCaseId);
        Assert.Equal("Acme Industrial", request.VendorName);
        Assert.Equal("INV-2026-901", request.InvoiceNumber);
        Assert.Equal(125.00m, request.InvoiceUnitPrice);
        Assert.Equal("EUR", request.InvoiceCurrency);
        Assert.Equal("PO-4401", request.PurchaseOrderNumber);
        Assert.Equal(100.00m, request.PurchaseOrderUnitPrice);
        Assert.Equal("EUR", request.PurchaseOrderCurrency);
        Assert.Equal(25.00m, request.VariancePercent);
        Assert.Equal(5.00m, request.AllowedTolerancePercent);
        Assert.Equal("PriceTolerance", request.RuleType);
        Assert.Single(request.Evidences);
    }

    private sealed class StubChatClient(string responseText, string? modelId = null) : IChatClient
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
            var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText))
            {
                ModelId = modelId
            };
            return Task.FromResult(response);
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

    private sealed class ThrowingChatClient(Exception exception) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<ChatResponse>(exception);
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
