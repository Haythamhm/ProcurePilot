using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.UnitTests;

public sealed class TenantTests
{
    [Fact]
    public void NewTenantIsActiveAndNormalizesItsName()
    {
        var tenant = new Tenant(Guid.NewGuid(), "  Northwind  ");

        Assert.Equal("Northwind", tenant.Name);
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Fact]
    public void TenantCanBeDeactivatedAndReactivated()
    {
        var tenant = new Tenant(Guid.NewGuid(), "Northwind");

        tenant.Deactivate();
        Assert.Equal(TenantStatus.Inactive, tenant.Status);

        tenant.Activate();
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }
}
