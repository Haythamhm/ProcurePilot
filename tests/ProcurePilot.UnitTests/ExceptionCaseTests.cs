using ProcurePilot.Domain.Exceptions;
using ProcurePilot.Domain.Invoices;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Domain.Purchasing;

namespace ProcurePilot.UnitTests;

public sealed class ExceptionCaseTests
{
    private static readonly DateTimeOffset EvaluatedAtUtc =
        new(2026, 10, 8, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FailedMatchCreatesTenantScopedNewCaseWithRequiredReferences()
    {
        var fixture = CreateFixture(failed: true);
        var openedAt = EvaluatedAtUtc.AddMinutes(1);

        var exceptionCase = ExceptionCase.CreateFromFailedMatch(
            Guid.NewGuid(),
            fixture.InvoiceLine,
            fixture.PurchaseOrderLine,
            fixture.MatchResult,
            openedAt);

        Assert.Equal(fixture.TenantId, exceptionCase.TenantId);
        Assert.Equal(fixture.InvoiceLine.InvoiceId, exceptionCase.InvoiceId);
        Assert.Equal(fixture.PurchaseOrderLine.PurchaseOrderId, exceptionCase.PurchaseOrderId);
        Assert.Equal(fixture.MatchResult.Id, exceptionCase.MatchResultId);
        Assert.Equal(MatchExceptionCode.PriceVariance, exceptionCase.Type);
        Assert.Equal(ExceptionCaseStatus.New, exceptionCase.Status);
        Assert.Equal(openedAt, exceptionCase.OpenedAtUtc);
        Assert.Null(exceptionCase.ResolvedAtUtc);
    }

    [Fact]
    public void DefinedWorkflowTransitionsAreAccepted()
    {
        var exceptionCase = CreateExceptionCase();
        var timestamp = exceptionCase.OpenedAtUtc;

        Transition(exceptionCase, ExceptionCaseStatus.Analyzing);
        Transition(exceptionCase, ExceptionCaseStatus.Open);
        Transition(exceptionCase, ExceptionCaseStatus.WaitingAction);
        Transition(exceptionCase, ExceptionCaseStatus.Open);
        Transition(exceptionCase, ExceptionCaseStatus.WaitingExternal);
        Transition(exceptionCase, ExceptionCaseStatus.Open);
        exceptionCase.TransitionTo(ExceptionCaseStatus.Resolved, timestamp.AddMinutes(1));
        exceptionCase.TransitionTo(ExceptionCaseStatus.Closed, timestamp.AddMinutes(2));

        Assert.Equal(ExceptionCaseStatus.Closed, exceptionCase.Status);
        Assert.Equal(timestamp.AddMinutes(1), exceptionCase.ResolvedAtUtc);
    }

    [Theory]
    [InlineData(ExceptionCaseStatus.WaitingAction)]
    [InlineData(ExceptionCaseStatus.WaitingExternal)]
    public void WaitingStatesCanResolve(ExceptionCaseStatus waitingStatus)
    {
        var exceptionCase = CreateExceptionCase();
        Transition(exceptionCase, ExceptionCaseStatus.Analyzing);
        Transition(exceptionCase, ExceptionCaseStatus.Open);
        Transition(exceptionCase, waitingStatus);

        exceptionCase.TransitionTo(
            ExceptionCaseStatus.Resolved,
            exceptionCase.OpenedAtUtc.AddMinutes(1));

        Assert.Equal(ExceptionCaseStatus.Resolved, exceptionCase.Status);
        Assert.NotNull(exceptionCase.ResolvedAtUtc);
    }

    [Fact]
    public void InvalidTransitionIsRejectedWithoutChangingState()
    {
        var exceptionCase = CreateExceptionCase();

        var error = Assert.Throws<InvalidOperationException>(() =>
            exceptionCase.TransitionTo(
                ExceptionCaseStatus.Resolved,
                exceptionCase.OpenedAtUtc.AddMinutes(1)));

        Assert.Contains("New to Resolved", error.Message, StringComparison.Ordinal);
        Assert.Equal(ExceptionCaseStatus.New, exceptionCase.Status);
        Assert.Null(exceptionCase.ResolvedAtUtc);
    }

    [Fact]
    public void PassedMatchCannotCreateExceptionCase()
    {
        var fixture = CreateFixture(failed: false);

        Assert.Throws<InvalidOperationException>(() =>
            ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                fixture.MatchResult,
                EvaluatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void MismatchedOrCrossTenantInputsAreRejected()
    {
        var fixture = CreateFixture(failed: true);
        var otherFixture = CreateFixture(failed: true);

        Assert.Throws<InvalidOperationException>(() =>
            ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                otherFixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                fixture.MatchResult,
                EvaluatedAtUtc.AddMinutes(1)));

        Assert.Throws<InvalidOperationException>(() =>
            ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                otherFixture.PurchaseOrderLine,
                fixture.MatchResult,
                EvaluatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void LifecycleTimestampsCannotPredateTheirSource()
    {
        var fixture = CreateFixture(failed: true);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                fixture.MatchResult,
                EvaluatedAtUtc.AddTicks(-1)));

        var exceptionCase = CreateExceptionCase();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            exceptionCase.TransitionTo(
                ExceptionCaseStatus.Analyzing,
                exceptionCase.OpenedAtUtc.AddTicks(-1)));
    }

    private static void Transition(ExceptionCase exceptionCase, ExceptionCaseStatus status) =>
        exceptionCase.TransitionTo(status, exceptionCase.OpenedAtUtc);

    private static ExceptionCase CreateExceptionCase()
    {
        var fixture = CreateFixture(failed: true);
        return ExceptionCase.CreateFromFailedMatch(
            Guid.NewGuid(),
            fixture.InvoiceLine,
            fixture.PurchaseOrderLine,
            fixture.MatchResult,
            EvaluatedAtUtc.AddMinutes(1));
    }

    private static Fixture CreateFixture(bool failed)
    {
        var tenantId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var purchaseOrderPrice = 100m;
        var invoicePrice = failed ? 105m : 102m;
        var invoice = new Invoice(
            Guid.NewGuid(),
            tenantId,
            vendorId,
            "INV-EXCEPTION-1",
            new DateOnly(2026, 10, 8),
            "USD",
            invoicePrice);
        var invoiceLine = invoice.AddLine(
            Guid.NewGuid(),
            1,
            "Exception item",
            1m,
            invoicePrice);
        var purchaseOrder = new PurchaseOrder(
            Guid.NewGuid(),
            tenantId,
            vendorId,
            "PO-EXCEPTION-1",
            new DateOnly(2026, 10, 1),
            "USD",
            "Open",
            purchaseOrderPrice);
        var purchaseOrderLine = purchaseOrder.AddLine(
            Guid.NewGuid(),
            1,
            "Exception item",
            1m,
            purchaseOrderPrice);
        var rule = new MatchingRule(
            Guid.NewGuid(),
            tenantId,
            MatchingRuleType.PriceVariance,
            3m,
            EvaluatedAtUtc.AddMinutes(-1));
        var matchResult = MatchResult.EvaluatePriceVariance(
            Guid.NewGuid(),
            invoiceLine,
            purchaseOrderLine,
            rule,
            EvaluatedAtUtc);

        return new Fixture(tenantId, invoiceLine, purchaseOrderLine, matchResult);
    }

    private sealed record Fixture(
        Guid TenantId,
        InvoiceLine InvoiceLine,
        PurchaseOrderLine PurchaseOrderLine,
        MatchResult MatchResult);
}
