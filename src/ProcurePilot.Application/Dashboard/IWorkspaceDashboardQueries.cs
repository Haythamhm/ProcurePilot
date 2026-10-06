namespace ProcurePilot.Application.Dashboard;

public interface IWorkspaceDashboardQueries
{
    Task<WorkspaceDashboardReadModel> GetAsync(
        CancellationToken cancellationToken = default);
}

public sealed record WorkspaceDashboardReadModel(
    int PurchaseOrderCount,
    int InvoiceCount,
    int ReviewRequiredCount,
    int ReadyInvoiceCount,
    IReadOnlyList<DashboardInvoiceReadModel> RecentInvoices);

public sealed record DashboardInvoiceReadModel(
    Guid Id,
    string FileName,
    string Status,
    DateTimeOffset UploadedAtUtc);
