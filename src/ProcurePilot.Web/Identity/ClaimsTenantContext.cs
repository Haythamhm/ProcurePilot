using ProcurePilot.Application.Tenancy;

namespace ProcurePilot.Web.Identity;

public sealed class ClaimsTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid? TenantId
    {
        get
        {
            var values = httpContextAccessor.HttpContext?.User
                .FindAll(TenantClaimTypes.TenantId)
                .Select(claim => claim.Value)
                .ToArray();

            return values is { Length: 1 } && Guid.TryParse(values[0], out var tenantId)
                ? tenantId
                : null;
        }
    }

    public Guid RequireTenantId() => TenantId
        ?? throw new InvalidOperationException("An authenticated tenant context is required.");
}
