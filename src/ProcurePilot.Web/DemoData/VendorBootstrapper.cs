using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Vendors;
using ProcurePilot.Infrastructure.Persistence;
using ProcurePilot.Web.Identity;

namespace ProcurePilot.Web.DemoData;

public sealed class VendorBootstrapper(
    IOptions<IdentityBootstrapOptions> identityOptions,
    ProcurePilotDbContext provisioningDbContext,
    DbContextOptions<ProcurePilotDbContext> dbContextOptions)
{
    public const string AtlasExternalReference = "ATLAS-MFG";
    public const string AtlasName = "Atlas Manufacturing";
    public const string AtlasTaxIdentifier = "DEMO-ATLAS-MFG";

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
                "The demo vendor cannot be provisioned before its configured tenant exists.");
        }

        await using var tenantDbContext = new ProcurePilotDbContext(
            dbContextOptions,
            new ProvisioningTenantContext(tenantId));

        var atlas = await tenantDbContext.Vendors
            .SingleOrDefaultAsync(
                vendor => vendor.ExternalReference == AtlasExternalReference,
                cancellationToken);

        if (atlas is null)
        {
            tenantDbContext.Vendors.Add(new Vendor(
                Guid.NewGuid(),
                tenantId,
                AtlasExternalReference,
                AtlasName,
                AtlasTaxIdentifier));
            await tenantDbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (atlas.Name != AtlasName || atlas.TaxIdentifier != AtlasTaxIdentifier)
        {
            throw new InvalidOperationException(
                "The Atlas demo vendor reference is already used by different data.");
        }
    }

    private sealed class ProvisioningTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid? TenantId { get; } = tenantId;

        public Guid RequireTenantId() => tenantId;
    }
}
