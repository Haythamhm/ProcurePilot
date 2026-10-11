using ProcurePilot.Application.AI.Evaluations;

namespace ProcurePilot.UnitTests.AiEvals;

public sealed class AiEvalDatasetTests
{
    [Fact]
    public void AllCases_ContainsExactlySixReferenceCases()
    {
        var cases = AiEvalDataset.AllCases;

        Assert.Equal(6, cases.Count);
        Assert.Equal("1.0.0", AiEvalDataset.DatasetVersion);
        Assert.Equal("AT-AI-001", AiEvalDataset.TraceabilityId);
    }

    [Theory]
    [InlineData("REF-001")]
    [InlineData("REF-002")]
    [InlineData("REF-003")]
    [InlineData("REF-004")]
    [InlineData("REF-005")]
    [InlineData("REF-006")]
    public void AllCases_ContainsExpectedCaseIds(string caseId)
    {
        var found = AiEvalDataset.GetCaseById(caseId);

        Assert.NotNull(found);
        Assert.Equal(caseId, found.CaseId);
        Assert.False(string.IsNullOrWhiteSpace(found.Title));
        Assert.False(string.IsNullOrWhiteSpace(found.Description));
        Assert.NotNull(found.Request);
        Assert.NotEmpty(found.Request.VendorName);
        Assert.NotEmpty(found.Request.Evidences);
        Assert.NotNull(found.GroundTruth);
        Assert.NotEmpty(found.GroundTruth.ExpectedRootCauseCategory);
        Assert.NotEmpty(found.GroundTruth.ExpectedPrimaryAction);
        Assert.NotEmpty(found.GroundTruth.RequiredFactualKeywords);
        Assert.NotNull(found.HallucinationRules);
        Assert.NotEmpty(found.HallucinationRules.ForbiddenPatterns);
        Assert.NotEmpty(found.HallucinationRules.ForbiddenInventedAmounts);
        Assert.NotEmpty(found.HallucinationRules.ForbiddenDecisionTerms);
    }

    [Fact]
    public void GetCaseById_ReturnsNull_WhenCaseDoesNotExist()
    {
        var found = AiEvalDataset.GetCaseById("REF-999");
        Assert.Null(found);
    }

    [Fact]
    public void AllCases_HaveUniqueCaseIdsAndNonZeroPrices()
    {
        var cases = AiEvalDataset.AllCases;
        var uniqueIds = cases.Select(c => c.CaseId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(cases.Count, uniqueIds.Count);

        foreach (var c in cases)
        {
            Assert.True(c.Request.InvoiceUnitPrice > 0, $"{c.CaseId} should have positive invoice price");
            Assert.True(c.Request.PurchaseOrderUnitPrice > 0, $"{c.CaseId} should have positive PO price");
            Assert.True(c.Request.VariancePercent >= 0, $"{c.CaseId} should have non-negative variance");
            Assert.True(c.Request.AllowedTolerancePercent > 0, $"{c.CaseId} should have positive tolerance threshold");
            Assert.NotEmpty(c.Request.Evidences);
        }
    }
}
