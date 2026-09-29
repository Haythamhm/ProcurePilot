namespace ProcurePilot.Application.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }

    Guid RequireTenantId();
}
