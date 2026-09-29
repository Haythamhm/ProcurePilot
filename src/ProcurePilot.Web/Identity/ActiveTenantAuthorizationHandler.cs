using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Tenancy;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Web.Identity;

public sealed class ActiveTenantAuthorizationHandler(
    ITenantContext tenantContext,
    ProcurePilotDbContext dbContext)
    : AuthorizationHandler<ActiveTenantRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveTenantRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true || tenantContext.TenantId is not { } tenantId)
        {
            return;
        }

        var tenantIsActive = await dbContext.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(tenant => tenant.Id == tenantId && tenant.Status == TenantStatus.Active);

        if (tenantIsActive)
        {
            context.Succeed(requirement);
        }
    }
}
