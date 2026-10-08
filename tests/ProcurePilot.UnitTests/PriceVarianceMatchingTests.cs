using ProcurePilot.Domain.Invoices;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Domain.Purchasing;

namespace ProcurePilot.UnitTests;

public sealed class PriceVarianceMatchingTests
{
    private static readonly DateTimeOffset EffectiveFromUtc =
        new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FivePercentVarianceFailsAndRaisesPriceVariance()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 2520m, purchaseOrderUnitPrice: 2400m);

        var result = Evaluate(fixture, tolerancePercent: 3m);

        Assert.Equal(5m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Failed, result.Status);
        Assert.Equal(MatchExceptionCode.PriceVariance, result.ExceptionCode);
        AssertSnapshots(fixture, result, expectedTolerance: 3m);
    }

    [Fact]
    public void TwoPercentVariancePassesWithoutException()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 2448m, purchaseOrderUnitPrice: 2400m);

        var result = Evaluate(fixture, tolerancePercent: 3m);

        Assert.Equal(2m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Passed, result.Status);
        Assert.Null(result.ExceptionCode);
    }

    [Fact]
    public void VarianceExactlyAtTolerancePasses()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 103m, purchaseOrderUnitPrice: 100m);

        var result = Evaluate(fixture, tolerancePercent: 3m);

        Assert.Equal(3m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Passed, result.Status);
        Assert.Null(result.ExceptionCode);
    }

    [Fact]
    public void SamePricesPassWithZeroTolerance()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 100m, purchaseOrderUnitPrice: 100m);

        var result = Evaluate(fixture, tolerancePercent: 0m);

        Assert.Equal(0m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Passed, result.Status);
        Assert.Null(result.ExceptionCode);
    }

    [Fact]
    public void NegativeVarianceExactlyAtTolerancePasses()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 97m, purchaseOrderUnitPrice: 100m);

        var result = Evaluate(fixture, tolerancePercent: 3m);

        Assert.Equal(-3m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Passed, result.Status);
        Assert.Null(result.ExceptionCode);
    }

    [Fact]
    public void HundredPercentVariancePassesAtMaximumTolerance()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 200m, purchaseOrderUnitPrice: 100m);

        var result = Evaluate(fixture, tolerancePercent: 100m);

        Assert.Equal(100m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Passed, result.Status);
        Assert.Null(result.ExceptionCode);
    }

    [Fact]
    public void NegativeVarianceUsesItsAbsoluteValueForTheDecision()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 95m, purchaseOrderUnitPrice: 100m);

        var result = Evaluate(fixture, tolerancePercent: 3m);

        Assert.Equal(-5m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Failed, result.Status);
        Assert.Equal(MatchExceptionCode.PriceVariance, result.ExceptionCode);
    }

    [Fact]
    public void ResultRoundsStoredVarianceButEvaluatesTheExactAmount()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 301m, purchaseOrderUnitPrice: 300m);

        var result = Evaluate(fixture, tolerancePercent: 0.3333m);

        Assert.Equal(0.3333m, result.VariancePercent);
        Assert.Equal(MatchResultStatus.Failed, result.Status);
    }

    [Fact]
    public void ZeroPurchaseOrderPriceCannotProduceAnInventedPercentage()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 10m, purchaseOrderUnitPrice: 0m);

        var error = Assert.Throws<InvalidOperationException>(() => Evaluate(fixture, 3m));

        Assert.Contains("zero", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CrossTenantInputsAreRejected()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 105m, purchaseOrderUnitPrice: 100m);
        var otherTenantRule = new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            3m,
            EffectiveFromUtc);

        var error = Assert.Throws<InvalidOperationException>(() =>
            MatchResult.EvaluatePriceVariance(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                otherTenantRule,
                EffectiveFromUtc));

        Assert.Contains("same tenant", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuleMustBeEffectiveAtEvaluationTime()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 105m, purchaseOrderUnitPrice: 100m);
        var rule = new MatchingRule(
            Guid.NewGuid(),
            fixture.TenantId,
            MatchingRuleType.PriceVariance,
            3m,
            EffectiveFromUtc);

        Assert.Throws<InvalidOperationException>(() =>
            MatchResult.EvaluatePriceVariance(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                rule,
                EffectiveFromUtc.AddTicks(-1)));

        var nextRule = rule.CreateNextVersion(Guid.NewGuid(), 4m, EffectiveFromUtc.AddDays(1));
        Assert.Throws<InvalidOperationException>(() =>
            MatchResult.EvaluatePriceVariance(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                rule,
                nextRule.EffectiveFromUtc));
    }

    private static MatchResult Evaluate(Fixture fixture, decimal tolerancePercent)
    {
        var rule = new MatchingRule(
            Guid.NewGuid(),
            fixture.TenantId,
            MatchingRuleType.PriceVariance,
            tolerancePercent,
            EffectiveFromUtc);

        return MatchResult.EvaluatePriceVariance(
            Guid.NewGuid(),
            fixture.InvoiceLine,
            fixture.PurchaseOrderLine,
            rule,
            EffectiveFromUtc.AddMinutes(1));
    }

    private static Fixture CreateFixture(decimal invoiceUnitPrice, decimal purchaseOrderUnitPrice)
    {
        var tenantId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var invoice = new Invoice(
            Guid.NewGuid(),
            tenantId,
            vendorId,
            "INV-MATCH-1",
            new DateOnly(2026, 10, 6),
            "USD",
            invoiceUnitPrice);
        var invoiceLine = invoice.AddLine(
            Guid.NewGuid(),
            1,
            "Matched item",
            1m,
            invoiceUnitPrice);
        var purchaseOrder = new PurchaseOrder(
            Guid.NewGuid(),
            tenantId,
            vendorId,
            "PO-MATCH-1",
            new DateOnly(2026, 10, 1),
            "USD",
            "Open",
            purchaseOrderUnitPrice);
        var purchaseOrderLine = purchaseOrder.AddLine(
            Guid.NewGuid(),
            1,
            "Matched item",
            1m,
            purchaseOrderUnitPrice);

        return new Fixture(tenantId, invoiceLine, purchaseOrderLine);
    }

    private static void AssertSnapshots(Fixture fixture, MatchResult result, decimal expectedTolerance)
    {
        Assert.Equal(fixture.TenantId, result.TenantId);
        Assert.Equal(fixture.InvoiceLine.Id, result.InvoiceLineId);
        Assert.Equal(fixture.PurchaseOrderLine.Id, result.PurchaseOrderLineId);
        Assert.Equal(fixture.InvoiceLine.UnitPrice, result.InvoiceUnitPrice);
        Assert.Equal(fixture.PurchaseOrderLine.UnitPrice, result.PurchaseOrderUnitPrice);
        Assert.Equal(expectedTolerance, result.AllowedTolerancePercent);
        Assert.Equal(1, result.MatchingRuleVersion);
        Assert.Equal(TimeSpan.Zero, result.EvaluatedAtUtc.Offset);
    }

    private sealed record Fixture(
        Guid TenantId,
        InvoiceLine InvoiceLine,
        PurchaseOrderLine PurchaseOrderLine);
}
