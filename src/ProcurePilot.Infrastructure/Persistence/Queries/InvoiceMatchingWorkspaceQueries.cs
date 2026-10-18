using Microsoft.EntityFrameworkCore;
using ProcurePilot.Domain.Invoices;
using ProcurePilot.Application.Matching;
using ProcurePilot.Domain.Matching;

namespace ProcurePilot.Infrastructure.Persistence.Queries;

internal sealed class InvoiceMatchingWorkspaceQueries(
    ProcurePilotDbContext dbContext,
    TimeProvider timeProvider) : IInvoiceMatchingWorkspaceQueries
{
    public async Task<InvoiceMatchingWorkspaceReadModel?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return null;
        }

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Where(value => value.Id == invoiceId && value.ProcessingStatus == InvoiceProcessingStatus.Ready)
            .Select(value => new { value.Id, value.VendorId, value.Currency })
            .SingleOrDefaultAsync(cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var invoiceLines = await dbContext.InvoiceLines
            .AsNoTracking()
            .Where(value => value.InvoiceId == invoiceId)
            .OrderBy(value => value.LineNumber)
            .Select(value => new InvoiceMatchingLineReadModel(
                value.Id,
                value.LineNumber,
                value.Description,
                value.UnitPrice))
            .ToListAsync(cancellationToken);

        var vendorName = await dbContext.Vendors
            .AsNoTracking()
            .Where(value => value.Id == invoice.VendorId)
            .Select(value => value.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var purchaseOrderEntities = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(value => value.Lines)
            .Where(value => value.VendorId == invoice.VendorId && value.Currency == invoice.Currency)
            .OrderBy(value => value.Number)
            .ToListAsync(cancellationToken);
        var purchaseOrders = purchaseOrderEntities
            .Select(order => new InvoiceMatchingPurchaseOrderReadModel(
                order.Id,
                order.Number,
                vendorName ?? string.Empty,
                order.Currency,
                order.Lines.OrderBy(line => line.LineNumber)
                    .Select(line => new InvoiceMatchingPurchaseOrderLineReadModel(
                        line.Id,
                        line.LineNumber,
                        line.Description,
                        line.UnitPrice))
                    .ToArray()))
            .ToArray();

        var now = timeProvider.GetUtcNow();
        var rules = await dbContext.MatchingRules
            .AsNoTracking()
            .Where(value => value.Type == MatchingRuleType.PriceVariance
                && value.EffectiveFromUtc <= now
                && (value.EffectiveToUtc == null || value.EffectiveToUtc > now))
            .OrderByDescending(value => value.Version)
            .Select(value => new InvoiceMatchingRuleReadModel(
                value.Id,
                value.Version,
                value.AllowedTolerancePercent))
            .ToListAsync(cancellationToken);

        return new InvoiceMatchingWorkspaceReadModel(invoice.Id, invoiceLines, purchaseOrders, rules);
    }
}
