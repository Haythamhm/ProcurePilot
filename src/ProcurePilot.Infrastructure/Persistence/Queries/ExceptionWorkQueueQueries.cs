using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Domain.Exceptions;

namespace ProcurePilot.Infrastructure.Persistence.Queries;

internal sealed class ExceptionWorkQueueQueries(ProcurePilotDbContext dbContext)
    : IExceptionWorkQueueQueries
{
    public async Task<IReadOnlyList<ExceptionWorkQueueItem>> ListAsync(
        ExceptionWorkQueueFilter filter,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter));
        }

        if (filter is ExceptionWorkQueueFilter.AssignedToMe &&
            (!currentUserId.HasValue || currentUserId.Value == Guid.Empty))
        {
            throw new ArgumentException(
                "The current user is required for the assigned queue.",
                nameof(currentUserId));
        }

        var cases = dbContext.ExceptionCases.AsNoTracking();
        cases = filter switch
        {
            ExceptionWorkQueueFilter.Open => cases.Where(exceptionCase =>
                exceptionCase.Status != ExceptionCaseStatus.Resolved &&
                exceptionCase.Status != ExceptionCaseStatus.Closed),
            ExceptionWorkQueueFilter.Resolved => cases.Where(exceptionCase =>
                exceptionCase.Status == ExceptionCaseStatus.Resolved ||
                exceptionCase.Status == ExceptionCaseStatus.Closed),
            ExceptionWorkQueueFilter.AssignedToMe => cases.Where(exceptionCase =>
                exceptionCase.AssignedToUserId == currentUserId &&
                exceptionCase.Status != ExceptionCaseStatus.Resolved &&
                exceptionCase.Status != ExceptionCaseStatus.Closed),
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };

        return await (
                from exceptionCase in cases
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
                    exceptionCase.Severity.ToString(),
                    exceptionCase.AssignedToUserId,
                    exceptionCase.OpenedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
