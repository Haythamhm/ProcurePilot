using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Domain.Exceptions;

namespace ProcurePilot.Infrastructure.Persistence.Queries;

internal sealed class ExceptionWorkQueueQueries(ProcurePilotDbContext dbContext)
    : IExceptionWorkQueueQueries
{
    public async Task<IReadOnlyList<ExceptionWorkQueueItem>> ListOpenAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
                from exceptionCase in dbContext.ExceptionCases.AsNoTracking()
                where exceptionCase.Status != ExceptionCaseStatus.Resolved
                    && exceptionCase.Status != ExceptionCaseStatus.Closed
                join invoice in dbContext.Invoices.AsNoTracking()
                    on new { exceptionCase.TenantId, exceptionCase.InvoiceId }
                    equals new { invoice.TenantId, InvoiceId = invoice.Id }
                join vendor in dbContext.Vendors.AsNoTracking()
                    on new { invoice.TenantId, invoice.VendorId }
                    equals new { vendor.TenantId, VendorId = (Guid?)vendor.Id }
                    into vendors
                from vendor in vendors.DefaultIfEmpty()
                orderby exceptionCase.OpenedAtUtc, exceptionCase.Id
                select new ExceptionWorkQueueItem(
                    exceptionCase.Id,
                    vendor == null ? "Vendor unavailable" : vendor.Name,
                    exceptionCase.Type.ToString(),
                    exceptionCase.Status.ToString(),
                    exceptionCase.OpenedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
