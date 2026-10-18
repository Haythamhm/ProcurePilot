namespace ProcurePilot.Application.Matching;

public interface IInvoiceMatchingWorkspaceQueries
{
    Task<InvoiceMatchingWorkspaceReadModel?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}

public sealed record InvoiceMatchingWorkspaceReadModel(
    Guid InvoiceId,
    IReadOnlyList<InvoiceMatchingLineReadModel> InvoiceLines,
    IReadOnlyList<InvoiceMatchingPurchaseOrderReadModel> PurchaseOrders,
    IReadOnlyList<InvoiceMatchingRuleReadModel> Rules);

public sealed record InvoiceMatchingLineReadModel(
    Guid Id,
    int LineNumber,
    string Description,
    decimal UnitPrice);

public sealed record InvoiceMatchingPurchaseOrderReadModel(
    Guid Id,
    string Number,
    string VendorName,
    string Currency,
    IReadOnlyList<InvoiceMatchingPurchaseOrderLineReadModel> Lines);

public sealed record InvoiceMatchingPurchaseOrderLineReadModel(
    Guid Id,
    int LineNumber,
    string Description,
    decimal UnitPrice);

public sealed record InvoiceMatchingRuleReadModel(
    Guid Id,
    int Version,
    decimal AllowedTolerancePercent);
