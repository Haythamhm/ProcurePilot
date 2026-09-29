namespace ProcurePilot.Domain.Tenancy;

public sealed class Tenant
{
    private Tenant()
    {
    }

    public Tenant(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
        Status = TenantStatus.Active;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; }

    public void Deactivate() => Status = TenantStatus.Inactive;

    public void Activate() => Status = TenantStatus.Active;
}
