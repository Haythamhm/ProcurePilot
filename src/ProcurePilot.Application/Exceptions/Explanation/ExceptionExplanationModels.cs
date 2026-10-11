namespace ProcurePilot.Application.Exceptions.Explanation;

public sealed record ExceptionExplanationRequest(
    Guid TenantId,
    Guid ExceptionCaseId,
    string VendorName,
    string InvoiceNumber,
    decimal InvoiceUnitPrice,
    string InvoiceCurrency,
    string PurchaseOrderNumber,
    decimal PurchaseOrderUnitPrice,
    string PurchaseOrderCurrency,
    decimal VariancePercent,
    decimal AllowedTolerancePercent,
    string RuleType,
    IReadOnlyList<ExceptionWorkspaceEvidenceReadModel> Evidences)
{
    public static ExceptionExplanationRequest FromWorkspaceModel(
        Guid tenantId,
        ExceptionWorkspaceReadModel workspaceModel)
    {
        ArgumentNullException.ThrowIfNull(workspaceModel);

        return new ExceptionExplanationRequest(
            tenantId,
            workspaceModel.Id,
            workspaceModel.VendorName,
            workspaceModel.InvoiceNumber,
            workspaceModel.InvoiceUnitPrice,
            workspaceModel.InvoiceCurrency,
            workspaceModel.PurchaseOrderNumber,
            workspaceModel.PurchaseOrderUnitPrice,
            workspaceModel.PurchaseOrderCurrency,
            workspaceModel.VariancePercent,
            workspaceModel.AllowedTolerancePercent,
            workspaceModel.MatchingRuleType,
            workspaceModel.Evidences);
    }
}

public sealed record ExceptionExplanationFacts(
    string VendorName,
    string InvoiceNumber,
    decimal InvoiceUnitPrice,
    string InvoiceCurrency,
    string PurchaseOrderNumber,
    decimal PurchaseOrderUnitPrice,
    string PurchaseOrderCurrency,
    decimal VariancePercent,
    decimal AllowedTolerancePercent,
    string RuleType);

public sealed record ExceptionExplanationInterpretation(
    string Summary,
    string RootCauseCategory,
    IReadOnlyList<string> ContributingFactors);

public sealed record ExceptionExplanationRecommendation(
    string PrimaryAction,
    IReadOnlyList<string> SuggestedSteps,
    string RiskAssessment);

public sealed record ExceptionExplanationEvidenceReference(
    string EvidenceType,
    string SourceReference,
    string Description);

public sealed record ExceptionExplanationResult(
    ExceptionExplanationFacts Facts,
    ExceptionExplanationInterpretation Interpretation,
    ExceptionExplanationRecommendation Recommendation,
    IReadOnlyList<ExceptionExplanationEvidenceReference> EvidenceReferences,
    string PromptVersion,
    string? ModelName,
    decimal ConfidenceScore,
    DateTimeOffset GeneratedAtUtc);
