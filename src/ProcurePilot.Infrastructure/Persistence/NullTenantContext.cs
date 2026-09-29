using ProcurePilot.Application.Tenancy;

namespace ProcurePilot.Infrastructure.Persistence;

internal sealed class NullTenantContext : ITenantContext
{
    public Guid? TenantId => null;

    public Guid RequireTenantId() =>
        throw new InvalidOperationException("An authenticated tenant context is required.");
}
