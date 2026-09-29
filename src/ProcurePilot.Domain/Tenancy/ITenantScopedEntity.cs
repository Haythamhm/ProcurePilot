namespace ProcurePilot.Domain.Tenancy;

public interface ITenantScopedEntity
{
    Guid TenantId { get; }
}
