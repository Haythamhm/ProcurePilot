using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Purchasing;

namespace ProcurePilot.Infrastructure.Persistence.Queries;

internal sealed class PurchaseOrderQueries(ProcurePilotDbContext dbContext)
    : IPurchaseOrderQueries
{
    public async Task<IReadOnlyList<PurchaseOrderReadModel>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var purchaseOrders = await (
                from purchaseOrder in dbContext.PurchaseOrders.AsNoTracking()
                join vendor in dbContext.Vendors.AsNoTracking()
                    on new { purchaseOrder.TenantId, Id = purchaseOrder.VendorId }
                    equals new { vendor.TenantId, vendor.Id }
                orderby purchaseOrder.OrderDate descending, purchaseOrder.Number
                select new PurchaseOrderReadModel(
                    purchaseOrder.Id,
                    purchaseOrder.Number,
                    vendor.Name,
                    purchaseOrder.OrderDate,
                    purchaseOrder.Currency,
                    purchaseOrder.Status,
                    purchaseOrder.TotalAmount,
                    purchaseOrder.Lines
                        .OrderBy(line => line.LineNumber)
                        .Select(line => new PurchaseOrderLineReadModel(
                            line.LineNumber,
                            line.Description,
                            line.Quantity,
                            line.UnitPrice))
                        .ToList()))
            .ToListAsync(cancellationToken);

        return purchaseOrders;
    }
}
