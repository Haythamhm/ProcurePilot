using ProcurePilot.Domain.Exceptions;

namespace ProcurePilot.UnitTests;

public sealed class AiAnalysisEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid AnalysisId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateSuccess_InitializesAllPropertiesAndTrimsStrings()
    {
        var analysis = AiAnalysis.CreateSuccess(
            AnalysisId,
            TenantId,
            CaseId,
            analysisType: " ExceptionExplanation ",
            model: " gpt-4o-mini ",
            promptVersion: " 2026-10-11.1 ",
            confidenceScore: 0.95m,
            summary: " Supplier unit price exceeded agreed PO tariff. ",
            rootCauseCategory: " Tariff Drift ",
            recommendedAction: " Issue credit request. ",
            riskAssessment: " Low operational impact. ",
            promptTokens: 420,
            completionTokens: 85,
            totalTokens: 505,
            durationMs: 850,
            rawPayloadJson: "{\"result\": true}",
            createdAtUtc: Now);

        Assert.Equal(AnalysisId, analysis.Id);
        Assert.Equal(TenantId, analysis.TenantId);
        Assert.Equal(CaseId, analysis.ExceptionCaseId);
        Assert.Equal("ExceptionExplanation", analysis.AnalysisType);
        Assert.Equal(AiAnalysisStatus.Success, analysis.Status);
        Assert.Equal("gpt-4o-mini", analysis.Model);
        Assert.Equal("2026-10-11.1", analysis.PromptVersion);
        Assert.Equal(0.95m, analysis.ConfidenceScore);
        Assert.Equal("Supplier unit price exceeded agreed PO tariff.", analysis.Summary);
        Assert.Equal("Tariff Drift", analysis.RootCauseCategory);
        Assert.Equal("Issue credit request.", analysis.RecommendedAction);
        Assert.Equal("Low operational impact.", analysis.RiskAssessment);
        Assert.Equal(420, analysis.PromptTokens);
        Assert.Equal(85, analysis.CompletionTokens);
        Assert.Equal(505, analysis.TotalTokens);
        Assert.Equal(850, analysis.DurationMs);
        Assert.Null(analysis.ErrorMessage);
        Assert.Equal("{\"result\": true}", analysis.RawPayloadJson);
        Assert.Equal(Now.ToUniversalTime(), analysis.CreatedAtUtc);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void CreateSuccess_ThrowsWhenConfidenceScoreIsOutOfRange(decimal score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiAnalysis.CreateSuccess(
                AnalysisId,
                TenantId,
                CaseId,
                "ExceptionExplanation",
                "gpt-4o-mini",
                "2026-10-11.1",
                score,
                "Summary",
                "RootCause",
                "Action",
                "Risk",
                10,
                10,
                20,
                100,
                null,
                Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateSuccess_ThrowsWhenSummaryOrActionIsEmpty(string blank)
    {
        Assert.Throws<ArgumentException>(() =>
            AiAnalysis.CreateSuccess(
                AnalysisId,
                TenantId,
                CaseId,
                "ExceptionExplanation",
                "gpt-4o-mini",
                "2026-10-11.1",
                0.9m,
                blank,
                "RootCause",
                "Action",
                "Risk",
                null, null, null, 100, null, Now));

        Assert.Throws<ArgumentException>(() =>
            AiAnalysis.CreateSuccess(
                AnalysisId,
                TenantId,
                CaseId,
                "ExceptionExplanation",
                "gpt-4o-mini",
                "2026-10-11.1",
                0.9m,
                "Summary",
                "RootCause",
                blank,
                "Risk",
                null, null, null, 100, null, Now));
    }

    [Fact]
    public void CreateFailure_InitializesFailureStateAndNullsOutputFields()
    {
        var analysis = AiAnalysis.CreateFailure(
            AnalysisId,
            TenantId,
            CaseId,
            "ExceptionExplanation",
            "gpt-4o-mini",
            "2026-10-11.1",
            errorMessage: " Upstream rate limit exceeded. ",
            durationMs: 350,
            createdAtUtc: Now);

        Assert.Equal(AiAnalysisStatus.Failed, analysis.Status);
        Assert.Equal("Upstream rate limit exceeded.", analysis.ErrorMessage);
        Assert.Equal(350, analysis.DurationMs);
        Assert.Null(analysis.ConfidenceScore);
        Assert.Null(analysis.Summary);
        Assert.Null(analysis.RecommendedAction);
        Assert.Null(analysis.PromptTokens);
        Assert.Null(analysis.CompletionTokens);
        Assert.Null(analysis.TotalTokens);
    }

    [Fact]
    public void CreateUnavailable_InitializesUnavailableState()
    {
        var analysis = AiAnalysis.CreateUnavailable(
            AnalysisId,
            TenantId,
            CaseId,
            "ExceptionExplanation",
            "unknown",
            "2026-10-11.1",
            errorMessage: "AI provider is disabled.",
            durationMs: 5,
            createdAtUtc: Now);

        Assert.Equal(AiAnalysisStatus.Unavailable, analysis.Status);
        Assert.Equal("AI provider is disabled.", analysis.ErrorMessage);
        Assert.Equal(5, analysis.DurationMs);
    }

    [Fact]
    public void ValidationCommon_ThrowsOnNegativeDurationOrEmptyIds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiAnalysis.CreateSuccess(
                AnalysisId,
                TenantId,
                CaseId,
                "ExceptionExplanation",
                "gpt-4o-mini",
                "2026-10-11.1",
                0.9m,
                "Summary",
                null,
                "Action",
                null,
                null, null, null,
                durationMs: -1,
                null,
                Now));

        Assert.Throws<ArgumentException>(() =>
            AiAnalysis.CreateSuccess(
                Guid.Empty,
                TenantId,
                CaseId,
                "ExceptionExplanation",
                "gpt-4o-mini",
                "2026-10-11.1",
                0.9m,
                "Summary",
                null,
                "Action",
                null,
                null, null, null, 100, null, Now));
    }
}
