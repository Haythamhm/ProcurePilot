using ProcurePilot.Domain.Purchasing;

namespace ProcurePilot.UnitTests;

public sealed class PurchaseOrderTests
{
    [Fact]
    public void NewPurchaseOrderNormalizesHeaderAndAddsLine()
    {
        var tenantId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var purchaseOrder = new PurchaseOrder(
            purchaseOrderId,
            tenantId,
            Guid.NewGuid(),
            "  PO-2026-1042  ",
            new DateOnly(2026, 1, 15),
            " usd ",
            " Open ",
            1250.00m);

        var line = purchaseOrder.AddLine(
            Guid.NewGuid(),
            1,
            "  Industrial control module  ",
            10.0000m,
            125.00m);

        Assert.Equal("PO-2026-1042", purchaseOrder.Number);
        Assert.Equal("USD", purchaseOrder.Currency);
        Assert.Equal("Open", purchaseOrder.Status);
        Assert.Equal(1250.00m, purchaseOrder.TotalAmount);
        Assert.Equal(tenantId, line.TenantId);
        Assert.Equal(purchaseOrderId, line.PurchaseOrderId);
        Assert.Equal("Industrial control module", line.Description);
    }

    [Fact]
    public void PurchaseOrderRejectsDuplicateLineNumber()
    {
        var purchaseOrder = CreatePurchaseOrder();
        purchaseOrder.AddLine(Guid.NewGuid(), 1, "First line", 1m, 10m);

        var error = Assert.Throws<InvalidOperationException>(() =>
            purchaseOrder.AddLine(Guid.NewGuid(), 1, "Duplicate line", 1m, 10m));

        Assert.Contains("already exists", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("EURO")]
    [InlineData("12A")]
    public void PurchaseOrderRequiresThreeLetterCurrency(string currency)
    {
        Assert.Throws<ArgumentException>(() => new PurchaseOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-1",
            new DateOnly(2026, 1, 15),
            currency,
            "Open",
            10m));
    }

    [Fact]
    public void PurchaseOrderRejectsInvalidFinancialPrecision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PurchaseOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PO-1",
            new DateOnly(2026, 1, 15),
            "USD",
            "Open",
            10.001m));

        var purchaseOrder = CreatePurchaseOrder();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            purchaseOrder.AddLine(Guid.NewGuid(), 1, "Line", 0m, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            purchaseOrder.AddLine(Guid.NewGuid(), 1, "Line", 1m, 10.001m));
    }

    private static PurchaseOrder CreatePurchaseOrder() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "PO-1",
        new DateOnly(2026, 1, 15),
        "USD",
        "Open",
        10m);
}
