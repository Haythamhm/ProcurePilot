using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Purchasing;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Web.Identity;

namespace ProcurePilot.Web.DemoData;

public sealed class PurchaseOrderBootstrapper(
    IOptions<IdentityBootstrapOptions> identityOptions,
    ProcurePilotDbContext provisioningDbContext,
    DbContextOptions<ProcurePilotDbContext> dbContextOptions)
{
    public const string DemoNumber = "PO-2026-1042";
    public const string DemoCurrency = "USD";
    public const string DemoStatus = "Open";
    public const decimal DemoTotalAmount = 1250.00m;
    public const string DemoLineDescription = "Industrial control module";
    public const decimal DemoLineQuantity = 10.0000m;
    public const decimal DemoLineUnitPrice = 125.00m;

    public static readonly DateOnly DemoOrderDate = new(2026, 1, 15);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var tenantName = identityOptions.Value.TenantName;
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return;
        }

        var normalizedTenantName = tenantName.Trim();
        var tenantId = await provisioningDbContext.Tenants
            .IgnoreQueryFilters()
            .Where(tenant => tenant.Name == normalizedTenantName)
            .Select(tenant => tenant.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The demo purchase order cannot be provisioned before its configured tenant exists.");
        }

        await using var tenantDbContext = new ProcurePilotDbContext(
            dbContextOptions,
            new ProvisioningTenantContext(tenantId));

        var vendorId = await tenantDbContext.Vendors
            .Where(vendor => vendor.ExternalReference == VendorBootstrapper.AtlasExternalReference)
            .Select(vendor => vendor.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (vendorId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The demo purchase order cannot be provisioned before the Atlas vendor exists.");
        }

        var existing = await tenantDbContext.PurchaseOrders
            .Include(purchaseOrder => purchaseOrder.Lines)
            .SingleOrDefaultAsync(
                purchaseOrder => purchaseOrder.Number == DemoNumber,
                cancellationToken);

        if (existing is null)
        {
            var purchaseOrder = new PurchaseOrder(
                Guid.NewGuid(),
                tenantId,
                vendorId,
                DemoNumber,
                DemoOrderDate,
                DemoCurrency,
                DemoStatus,
                DemoTotalAmount);
            purchaseOrder.AddLine(
                Guid.NewGuid(),
                1,
                DemoLineDescription,
                DemoLineQuantity,
                DemoLineUnitPrice);

            tenantDbContext.PurchaseOrders.Add(purchaseOrder);
            await tenantDbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var line = existing.Lines.SingleOrDefault();
        if (existing.VendorId != vendorId ||
            existing.OrderDate != DemoOrderDate ||
            existing.Currency != DemoCurrency ||
            existing.Status != DemoStatus ||
            existing.TotalAmount != DemoTotalAmount ||
            line is null ||
            line.LineNumber != 1 ||
            line.Description != DemoLineDescription ||
            line.Quantity != DemoLineQuantity ||
            line.UnitPrice != DemoLineUnitPrice)
        {
            throw new InvalidOperationException(
                "The demo purchase order number is already used by different data.");
        }
    }

    private sealed class ProvisioningTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;

        public Guid RequireTenantId() => tenantId;
    }
}
