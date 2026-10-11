using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ProcurePilot.Application.Exceptions.Explanation;

public sealed class ChatClientExceptionExplanationService(IChatClient chatClient) : IExceptionExplanationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ExceptionExplanationResult> ExplainAsync(
        ExceptionExplanationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var messages = new ChatMessage[]
        {
            new(ChatRole.System, ExceptionExplanationPrompt.SystemPrompt),
            new(ChatRole.User, ExceptionExplanationPrompt.BuildUserPrompt(request))
        };

        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema<ExceptionExplanationResult>(
                SerializerOptions,
                schemaName: "exception_explanation",
                schemaDescription: "Structured price variance exception explanation and operational recommendation.")
        };

        ChatResponse response;
        try
        {
            response = await chatClient.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            throw new ExceptionExplanationUnavailableException(
                "The AI explanation service is currently disabled in application configuration.",
                exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ExceptionExplanationUnavailableException(
                "The AI explanation provider was unavailable or rejected the request.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            throw new ExceptionExplanationException("The AI explanation provider returned an empty response.");
        }

        ExceptionExplanationResult result;
        try
        {
            result = JsonSerializer.Deserialize<ExceptionExplanationResult>(
                    response.Text,
                    SerializerOptions)
                ?? throw new ExceptionExplanationException(
                    "The AI explanation provider returned an empty JSON result.");
        }
        catch (JsonException exception)
        {
            throw new ExceptionExplanationException(
                "The AI explanation provider returned invalid structured data.",
                exception);
        }

        ValidateResult(result);

        return result with
        {
            PromptVersion = ExceptionExplanationPrompt.CurrentVersion,
            ModelName = response.ModelId ?? result.ModelName,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateRequest(ExceptionExplanationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(request));
        }

        if (request.ExceptionCaseId == Guid.Empty)
        {
            throw new ArgumentException("An exception case identifier is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.VendorName))
        {
            throw new ArgumentException("A vendor name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
        {
            throw new ArgumentException("An invoice number is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PurchaseOrderNumber))
        {
            throw new ArgumentException("A purchase order number is required.", nameof(request));
        }
    }

    private static void ValidateResult(ExceptionExplanationResult result)
    {
        if (result.Facts is null)
        {
            throw new ExceptionExplanationException("The explanation result must contain deterministic facts.");
        }

        if (result.Interpretation is null || string.IsNullOrWhiteSpace(result.Interpretation.Summary))
        {
            throw new ExceptionExplanationException("The explanation result must contain a non-empty interpretation summary.");
        }

        if (result.Recommendation is null || string.IsNullOrWhiteSpace(result.Recommendation.PrimaryAction))
        {
            throw new ExceptionExplanationException("The explanation result must contain a non-empty primary recommendation action.");
        }

        if (result.ConfidenceScore is < 0 or > 1)
        {
            throw new ExceptionExplanationException("The explanation confidence score must be between 0.0 and 1.0.");
        }
    }
}
