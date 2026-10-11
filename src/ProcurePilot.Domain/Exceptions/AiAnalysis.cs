using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Exceptions;

public sealed class AiAnalysis : ITenantScopedEntity
{
    public const int AnalysisTypeMaxLength = 64;
    public const int StatusMaxLength = 32;
    public const int ModelMaxLength = 128;
    public const int PromptVersionMaxLength = 64;
    public const int RootCauseCategoryMaxLength = 128;
    public const int SummaryMaxLength = 2000;
    public const int RecommendedActionMaxLength = 1000;
    public const int RiskAssessmentMaxLength = 1000;
    public const int ErrorMessageMaxLength = 2000;

    private AiAnalysis()
    {
    }

    private AiAnalysis(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        string analysisType,
        AiAnalysisStatus status,
        string model,
        string promptVersion,
        decimal? confidenceScore,
        string? summary,
        string? rootCauseCategory,
        string? recommendedAction,
        string? riskAssessment,
        int? promptTokens,
        int? completionTokens,
        int? totalTokens,
        long durationMs,
        string? errorMessage,
        string? rawPayloadJson,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        ExceptionCaseId = exceptionCaseId;
        AnalysisType = analysisType;
        Status = status;
        Model = model;
        PromptVersion = promptVersion;
        ConfidenceScore = confidenceScore;
        Summary = summary;
        RootCauseCategory = rootCauseCategory;
        RecommendedAction = recommendedAction;
        RiskAssessment = riskAssessment;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = totalTokens;
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
        RawPayloadJson = rawPayloadJson;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ExceptionCaseId { get; private set; }

    public string AnalysisType { get; private set; } = string.Empty;

    public AiAnalysisStatus Status { get; private set; }

    public string Model { get; private set; } = string.Empty;

    public string PromptVersion { get; private set; } = string.Empty;

    public decimal? ConfidenceScore { get; private set; }

    public string? Summary { get; private set; }

    public string? RootCauseCategory { get; private set; }

    public string? RecommendedAction { get; private set; }

    public string? RiskAssessment { get; private set; }

    public int? PromptTokens { get; private set; }

    public int? CompletionTokens { get; private set; }

    public int? TotalTokens { get; private set; }

    public long DurationMs { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? RawPayloadJson { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AiAnalysis CreateSuccess(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        string analysisType,
        string model,
        string promptVersion,
        decimal confidenceScore,
        string summary,
        string? rootCauseCategory,
        string recommendedAction,
        string? riskAssessment,
        int? promptTokens,
        int? completionTokens,
        int? totalTokens,
        long durationMs,
        string? rawPayloadJson,
        DateTimeOffset createdAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId, analysisType, model, promptVersion, durationMs);

        if (confidenceScore is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidenceScore),
                "The confidence score must be between 0.0 and 1.0.");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("A summary is required for a successful analysis.", nameof(summary));
        }

        if (string.IsNullOrWhiteSpace(recommendedAction))
        {
            throw new ArgumentException(
                "A recommended action is required for a successful analysis.",
                nameof(recommendedAction));
        }

        return new AiAnalysis(
            id,
            tenantId,
            exceptionCaseId,
            analysisType.Trim(),
            AiAnalysisStatus.Success,
            model.Trim(),
            promptVersion.Trim(),
            confidenceScore,
            summary.Trim(),
            rootCauseCategory?.Trim(),
            recommendedAction.Trim(),
            riskAssessment?.Trim(),
            promptTokens,
            completionTokens,
            totalTokens,
            durationMs,
            errorMessage: null,
            rawPayloadJson,
            createdAtUtc.ToUniversalTime());
    }

    public static AiAnalysis CreateFailure(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        string analysisType,
        string model,
        string promptVersion,
        string errorMessage,
        long durationMs,
        DateTimeOffset createdAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId, analysisType, model, promptVersion, durationMs);

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("An error message is required for a failed analysis.", nameof(errorMessage));
        }

        return new AiAnalysis(
            id,
            tenantId,
            exceptionCaseId,
            analysisType.Trim(),
            AiAnalysisStatus.Failed,
            model.Trim(),
            promptVersion.Trim(),
            confidenceScore: null,
            summary: null,
            rootCauseCategory: null,
            recommendedAction: null,
            riskAssessment: null,
            promptTokens: null,
            completionTokens: null,
            totalTokens: null,
            durationMs,
            errorMessage: errorMessage.Trim(),
            rawPayloadJson: null,
            createdAtUtc.ToUniversalTime());
    }

    public static AiAnalysis CreateUnavailable(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        string analysisType,
        string model,
        string promptVersion,
        string errorMessage,
        long durationMs,
        DateTimeOffset createdAtUtc)
    {
        ValidateCommon(id, tenantId, exceptionCaseId, analysisType, model, promptVersion, durationMs);

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException(
                "An error message is required for an unavailable analysis.",
                nameof(errorMessage));
        }

        return new AiAnalysis(
            id,
            tenantId,
            exceptionCaseId,
            analysisType.Trim(),
            AiAnalysisStatus.Unavailable,
            model.Trim(),
            promptVersion.Trim(),
            confidenceScore: null,
            summary: null,
            rootCauseCategory: null,
            recommendedAction: null,
            riskAssessment: null,
            promptTokens: null,
            completionTokens: null,
            totalTokens: null,
            durationMs,
            errorMessage: errorMessage.Trim(),
            rawPayloadJson: null,
            createdAtUtc.ToUniversalTime());
    }

    private static void ValidateCommon(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        string analysisType,
        string model,
        string promptVersion,
        long durationMs)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An analysis identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        if (exceptionCaseId == Guid.Empty)
        {
            throw new ArgumentException("An exception case identifier is required.", nameof(exceptionCaseId));
        }

        if (string.IsNullOrWhiteSpace(analysisType))
        {
            throw new ArgumentException("An analysis type is required.", nameof(analysisType));
        }

        if (analysisType.Length > AnalysisTypeMaxLength)
        {
            throw new ArgumentException(
                $"Analysis type cannot exceed {AnalysisTypeMaxLength} characters.",
                nameof(analysisType));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("A model identifier is required.", nameof(model));
        }

        if (model.Length > ModelMaxLength)
        {
            throw new ArgumentException(
                $"Model cannot exceed {ModelMaxLength} characters.",
                nameof(model));
        }

        if (string.IsNullOrWhiteSpace(promptVersion))
        {
            throw new ArgumentException("A prompt version is required.", nameof(promptVersion));
        }

        if (promptVersion.Length > PromptVersionMaxLength)
        {
            throw new ArgumentException(
                $"Prompt version cannot exceed {PromptVersionMaxLength} characters.",
                nameof(promptVersion));
        }

        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration cannot be negative.");
        }
    }
}
