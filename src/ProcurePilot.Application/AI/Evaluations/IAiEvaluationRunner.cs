using ProcurePilot.Application.Exceptions.Explanation;

namespace ProcurePilot.Application.AI.Evaluations;

/// <summary>
/// Service interface for evaluating AI explanations against ground truth and detecting critical hallucinations.
/// Traceable to AT-AI-001.
/// </summary>
public interface IAiEvaluationRunner
{
    /// <summary>
    /// Evaluates an AI explanation result against a specific reference case.
    /// </summary>
    AiEvalResult Evaluate(AiEvalCase evalCase, ExceptionExplanationResult explanation);

    /// <summary>
    /// Evaluates an AI explanation result against all cases in the dataset.
    /// </summary>
    IReadOnlyList<AiEvalResult> EvaluateDataset(
        IReadOnlyList<AiEvalCase> cases,
        Func<AiEvalCase, ExceptionExplanationResult> explanationProvider);
}
