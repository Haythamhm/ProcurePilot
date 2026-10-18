using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.AI;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Application.Exceptions.Explanation;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Exceptions;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Infrastructure.Exceptions;

public sealed class AiAnalysisService(
    DbContextOptions<ProcurePilotDbContext> dbContextOptions,
    ITenantContext tenantContext,
    IExceptionWorkspaceQueries workspaceQueries,
    IExceptionExplanationService? explanationService = null,
    TimeProvider? timeProvider = null) : IAiAnalysisService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<AiAnalysisReadModel?> GetLatestForCaseAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default)
    {
        if (exceptionCaseId == Guid.Empty)
        {
            return null;
        }

        await using var dbContext = CreateDbContext();
        var analysis = await dbContext.AiAnalyses
            .AsNoTracking()
            .Where(a => a.ExceptionCaseId == exceptionCaseId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return analysis is null ? null : ToReadModel(analysis);
    }

    public async Task<IReadOnlyList<AiAnalysisReadModel>> GetHistoryForCaseAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default)
    {
        if (exceptionCaseId == Guid.Empty)
        {
            return [];
        }

        await using var dbContext = CreateDbContext();
        var analyses = await dbContext.AiAnalyses
            .AsNoTracking()
            .Where(a => a.ExceptionCaseId == exceptionCaseId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return analyses.Select(ToReadModel).ToList();
    }

    public async Task<AiAnalysisReadModel> ExplainAndRecordAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default)
    {
        if (exceptionCaseId == Guid.Empty)
        {
            throw new ArgumentException("An exception case identifier is required.", nameof(exceptionCaseId));
        }

        var tenantId = tenantContext.RequireTenantId();
        var workspaceModel = await workspaceQueries.GetAsync(exceptionCaseId, cancellationToken)
            ?? throw new KeyNotFoundException("The exception case was not found for the active tenant.");

        var request = ExceptionExplanationRequest.FromWorkspaceModel(tenantId, workspaceModel);
        var startTimestamp = Stopwatch.GetTimestamp();
        var now = _timeProvider.GetUtcNow();

        AiAnalysis analysis;
        if (explanationService is null)
        {
            throw new ExceptionExplanationUnavailableException("An AI explanation service is required to generate explanations.");
        }

        try
        {
            var result = await explanationService.ExplainAsync(request, cancellationToken);
            var durationMs = result.Telemetry?.DurationMs ?? (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

            analysis = AiAnalysis.CreateSuccess(
                Guid.NewGuid(),
                tenantId,
                exceptionCaseId,
                "ExceptionExplanation",
                result.ModelName ?? "unknown",
                result.PromptVersion,
                result.ConfidenceScore,
                result.Interpretation.Summary,
                result.Interpretation.RootCauseCategory,
                result.Recommendation.PrimaryAction,
                result.Recommendation.RiskAssessment,
                result.Telemetry?.PromptTokens,
                result.Telemetry?.CompletionTokens,
                result.Telemetry?.TotalTokens,
                durationMs,
                result.RawPayloadJson,
                now);
        }
        catch (ExceptionExplanationUnavailableException ex)
        {
            var durationMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            analysis = AiAnalysis.CreateUnavailable(
                Guid.NewGuid(),
                tenantId,
                exceptionCaseId,
                "ExceptionExplanation",
                "unknown",
                ExceptionExplanationPrompt.CurrentVersion,
                ex.Message,
                durationMs,
                now);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var durationMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            analysis = AiAnalysis.CreateFailure(
                Guid.NewGuid(),
                tenantId,
                exceptionCaseId,
                "ExceptionExplanation",
                "unknown",
                ExceptionExplanationPrompt.CurrentVersion,
                ex.Message,
                durationMs,
                now);
        }

        await using var dbContext = CreateDbContext();
        dbContext.AiAnalyses.Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);

        AiTelemetry.RecordAnalysis(
            analysis.Status.ToString(),
            analysis.Model,
            analysis.AnalysisType,
            analysis.DurationMs,
            analysis.PromptTokens,
            analysis.CompletionTokens,
            analysis.TotalTokens);

        return ToReadModel(analysis);
    }

    private static AiAnalysisReadModel ToReadModel(AiAnalysis analysis) =>
        new(
            analysis.Id,
            analysis.TenantId,
            analysis.ExceptionCaseId,
            analysis.AnalysisType,
            analysis.Status.ToString(),
            analysis.Model,
            analysis.PromptVersion,
            analysis.ConfidenceScore,
            analysis.Summary,
            analysis.RootCauseCategory,
            analysis.RecommendedAction,
            analysis.RiskAssessment,
            analysis.PromptTokens,
            analysis.CompletionTokens,
            analysis.TotalTokens,
            analysis.DurationMs,
            analysis.ErrorMessage,
            analysis.CreatedAtUtc);

    private ProcurePilotDbContext CreateDbContext() =>
        new(dbContextOptions, tenantContext);
}
