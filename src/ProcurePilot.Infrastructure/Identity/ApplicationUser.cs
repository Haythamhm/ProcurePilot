using Microsoft.AspNetCore.Identity;

namespace ProcurePilot.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
