namespace ProcurePilot.Application.Exceptions.Explanation;

public interface IAiAnalysisService
{
    Task<AiAnalysisReadModel?> GetLatestForCaseAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiAnalysisReadModel>> GetHistoryForCaseAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default);

    Task<AiAnalysisReadModel> ExplainAndRecordAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default);
}
