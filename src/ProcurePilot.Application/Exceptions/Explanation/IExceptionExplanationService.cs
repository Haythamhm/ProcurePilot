namespace ProcurePilot.Application.Exceptions.Explanation;

public interface IExceptionExplanationService
{
    Task<ExceptionExplanationResult> ExplainAsync(
        ExceptionExplanationRequest request,
        CancellationToken cancellationToken = default);
}
