namespace ProcurePilot.Application.Exceptions.Explanation;

public sealed record AiAnalysisReadModel(
    Guid Id,
    Guid TenantId,
    Guid ExceptionCaseId,
    string AnalysisType,
    string Status,
    string Model,
    string PromptVersion,
    decimal? ConfidenceScore,
    string? Summary,
    string? RootCauseCategory,
    string? RecommendedAction,
    string? RiskAssessment,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens,
    long DurationMs,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc);

public sealed record AiAnalysisTelemetry(
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens,
    long DurationMs);
