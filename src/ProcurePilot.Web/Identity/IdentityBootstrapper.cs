using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using ProcurePilot.Application.Security;
using ProcurePilot.Domain.Tenancy;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Web.Identity;

public sealed class IdentityBootstrapper(
    IOptions<IdentityBootstrapOptions> options,
    ProcurePilotDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
{
    public const string AdministratorRole = "Administrator";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var bootstrap = options.Value;
        var values = new[]
        {
            bootstrap.TenantName,
            bootstrap.Email,
            bootstrap.Password,
            bootstrap.DisplayName
        };

        if (values.All(string.IsNullOrWhiteSpace))
        {
            return;
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                "Identity bootstrap configuration must provide TenantName, Email, Password, and DisplayName together.");
        }

        var tenantName = bootstrap.TenantName!.Trim();
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(value => value.Name == tenantName, cancellationToken);

        if (tenant is null)
        {
            tenant = new Tenant(Guid.NewGuid(), tenantName);
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (tenant.Status is TenantStatus.Inactive)
        {
            throw new InvalidOperationException("The configured bootstrap tenant is inactive.");
        }

        var role = await roleManager.FindByNameAsync(AdministratorRole);
        if (role is null)
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(AdministratorRole));
            EnsureSucceeded(roleResult, "create the Administrator role");
            role = await roleManager.FindByNameAsync(AdministratorRole);
        }

        var roleClaims = await roleManager.GetClaimsAsync(role!);
        var requiredPermissions = new[]
        {
            ProcurePilotPermissions.InvoiceUpload,
            ProcurePilotPermissions.InvoiceReview,
            ProcurePilotPermissions.ExceptionView,
            ProcurePilotPermissions.ExceptionOverride
        };
        foreach (var permission in requiredPermissions)
        {
            if (roleClaims.Any(claim =>
                    claim.Type == ProcurePilotPermissions.ClaimType &&
                    claim.Value == permission))
            {
                continue;
            }

            var permissionResult = await roleManager.AddClaimAsync(
                role!,
                new Claim(ProcurePilotPermissions.ClaimType, permission));
            EnsureSucceeded(permissionResult, $"grant the {permission} permission");
        }

        var user = await userManager.FindByEmailAsync(bootstrap.Email!.Trim());
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserName = bootstrap.Email.Trim(),
                Email = bootstrap.Email.Trim(),
                EmailConfirmed = true,
                DisplayName = bootstrap.DisplayName!.Trim()
            };

            var userResult = await userManager.CreateAsync(user, bootstrap.Password!);
            EnsureSucceeded(userResult, "create the bootstrap user");
        }
        else if (user.TenantId != tenant.Id)
        {
            throw new InvalidOperationException("The configured bootstrap user already belongs to another tenant.");
        }

        if (!await userManager.IsInRoleAsync(user, AdministratorRole))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AdministratorRole);
            EnsureSucceeded(roleResult, "assign the Administrator role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {operation}: {errors}");
    }
}
