namespace ProcurePilot.Application.Purchasing;

public interface IPurchaseOrderQueries
{
    Task<IReadOnlyList<PurchaseOrderReadModel>> ListAsync(
        CancellationToken cancellationToken = default);
}

public sealed record PurchaseOrderReadModel(
    Guid Id,
    string Number,
    string VendorName,
    DateOnly OrderDate,
    string Currency,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<PurchaseOrderLineReadModel> Lines);

public sealed record PurchaseOrderLineReadModel(
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice);
