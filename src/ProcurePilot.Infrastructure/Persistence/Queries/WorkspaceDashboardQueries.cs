using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Dashboard;
using ProcurePilot.Domain.Invoices;

namespace ProcurePilot.Infrastructure.Persistence.Queries;

internal sealed class WorkspaceDashboardQueries(ProcurePilotDbContext dbContext)
    : IWorkspaceDashboardQueries
{
    public async Task<WorkspaceDashboardReadModel> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var purchaseOrderCount = await dbContext.PurchaseOrders.CountAsync(cancellationToken);
        var invoiceSummary = await dbContext.Invoices
            .GroupBy(_ => 1)
            .Select(invoices => new
            {
                Total = invoices.Count(),
                ReviewRequired = invoices.Count(
                    invoice => invoice.ProcessingStatus == InvoiceProcessingStatus.ReviewRequired),
                Ready = invoices.Count(
                    invoice => invoice.ProcessingStatus == InvoiceProcessingStatus.Ready)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var recentInvoices = await (
                from invoice in dbContext.Invoices.AsNoTracking()
                join document in dbContext.InvoiceDocuments.AsNoTracking()
                    on new { invoice.TenantId, InvoiceId = invoice.Id }
                    equals new { document.TenantId, document.InvoiceId }
                orderby document.UploadedAtUtc descending
                select new DashboardInvoiceReadModel(
                    invoice.Id,
                    document.OriginalFileName,
                    invoice.ProcessingStatus.ToString(),
                    document.UploadedAtUtc))
            .Take(5)
            .ToListAsync(cancellationToken);

        return new WorkspaceDashboardReadModel(
            purchaseOrderCount,
            invoiceSummary?.Total ?? 0,
            invoiceSummary?.ReviewRequired ?? 0,
            invoiceSummary?.Ready ?? 0,
            recentInvoices);
    }
}
