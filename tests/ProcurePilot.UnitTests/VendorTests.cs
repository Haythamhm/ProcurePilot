using ProcurePilot.Domain.Vendors;

namespace ProcurePilot.UnitTests;

public sealed class VendorTests
{
    [Fact]
    public void NewVendorNormalizesMasterData()
    {
        var tenantId = Guid.NewGuid();

        var vendor = new Vendor(
            Guid.NewGuid(),
            tenantId,
            "  ATLAS-MFG  ",
            "  Atlas Manufacturing  ",
            "  DEMO-ATLAS-MFG  ");

        Assert.Equal(tenantId, vendor.TenantId);
        Assert.Equal("ATLAS-MFG", vendor.ExternalReference);
        Assert.Equal("Atlas Manufacturing", vendor.Name);
        Assert.Equal("DEMO-ATLAS-MFG", vendor.TaxIdentifier);
    }

    [Fact]
    public void EmptyOptionalTaxIdentifierIsStoredAsNull()
    {
        var vendor = new Vendor(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ATLAS-MFG",
            "Atlas Manufacturing",
            "   ");

        Assert.Null(vendor.TaxIdentifier);
    }

    [Fact]
    public void VendorRequiresTenantAndVendorIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => new Vendor(
            Guid.Empty,
            Guid.NewGuid(),
            "ATLAS-MFG",
            "Atlas Manufacturing"));
        Assert.Throws<ArgumentException>(() => new Vendor(
            Guid.NewGuid(),
            Guid.Empty,
            "ATLAS-MFG",
            "Atlas Manufacturing"));
    }

    [Fact]
    public void VendorRequiresReferenceAndName()
    {
        Assert.Throws<ArgumentException>(() => new Vendor(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " ",
            "Atlas Manufacturing"));
        Assert.Throws<ArgumentException>(() => new Vendor(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ATLAS-MFG",
            " "));
    }
}
