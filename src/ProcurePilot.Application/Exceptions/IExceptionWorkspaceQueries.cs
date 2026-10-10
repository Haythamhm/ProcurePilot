namespace ProcurePilot.Application.Exceptions;

public interface IExceptionWorkspaceQueries
{
    Task<ExceptionWorkspaceReadModel?> GetAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default);
}

public sealed record ExceptionWorkspaceReadModel(
    Guid Id,
    string Type,
    string Status,
    string Severity,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    string? AssignedToDisplayName,
    string VendorName,
    string InvoiceNumber,
    string InvoiceCurrency,
    int InvoiceLineNumber,
    string InvoiceLineDescription,
    decimal InvoiceQuantity,
    decimal InvoiceUnitPrice,
    string? InvoiceSourceFileName,
    string PurchaseOrderNumber,
    string PurchaseOrderCurrency,
    int PurchaseOrderLineNumber,
    string PurchaseOrderLineDescription,
    decimal PurchaseOrderQuantity,
    decimal PurchaseOrderUnitPrice,
    decimal VariancePercent,
    decimal AllowedTolerancePercent,
    string MatchingRuleType,
    int MatchingRuleVersion,
    DateTimeOffset EvaluatedAtUtc);
