using ProcurePilot.Application.Exceptions.Explanation;

namespace ProcurePilot.Application.AI.Evaluations;

/// <summary>
/// Represents ground truth expectations for an AI evaluation reference case.
/// </summary>
public sealed record AiEvalGroundTruth(
    string ExpectedRootCauseCategory,
    string ExpectedPrimaryAction,
    bool ExpectedWithinTolerance,
    IReadOnlyList<string> RequiredFactualKeywords);

/// <summary>
/// Represents hallucination and compliance detection rules for an evaluation case.
/// </summary>
public sealed record AiEvalHallucinationRules(
    IReadOnlyList<string> ForbiddenPatterns,
    IReadOnlyList<decimal> ForbiddenInventedAmounts,
    IReadOnlyList<string> ForbiddenDecisionTerms,
    bool StrictEvidenceGroundingOnly = true);

/// <summary>
/// Represents a versioned synthetic evaluation reference case (REF-001 through REF-006).
/// Traceable to acceptance test AT-AI-001.
/// </summary>
public sealed record AiEvalCase(
    string CaseId,
    string Title,
    string Description,
    ExceptionExplanationRequest Request,
    AiEvalGroundTruth GroundTruth,
    AiEvalHallucinationRules HallucinationRules);

/// <summary>
/// A specific hallucination or policy finding identified during evaluation.
/// </summary>
public sealed record AiEvalHallucinationFinding(
    string FindingType,
    string Severity,
    string Description,
    string Evidence);

/// <summary>
/// Outcome of evaluating an AI explanation against a reference evaluation case.
/// </summary>
public sealed record AiEvalResult(
    string CaseId,
    bool Passed,
    bool HasCriticalHallucinations,
    decimal FactualAccuracyScore,
    IReadOnlyList<AiEvalHallucinationFinding> Findings,
    DateTimeOffset EvaluatedAtUtc,
    string? ModelName = null,
    string? PromptVersion = null)
{
    public bool IsReleaseBlocker => HasCriticalHallucinations || !Passed;
}
