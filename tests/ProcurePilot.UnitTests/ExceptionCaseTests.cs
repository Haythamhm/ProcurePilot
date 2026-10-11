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
            ExceptionSeverity.Medium,
            openedAt);

        Assert.Equal(fixture.TenantId, exceptionCase.TenantId);
        Assert.Equal(fixture.InvoiceLine.InvoiceId, exceptionCase.InvoiceId);
        Assert.Equal(fixture.PurchaseOrderLine.PurchaseOrderId, exceptionCase.PurchaseOrderId);
        Assert.Equal(fixture.MatchResult.Id, exceptionCase.MatchResultId);
        Assert.Equal(MatchExceptionCode.PriceVariance, exceptionCase.Type);
        Assert.Equal(ExceptionCaseStatus.New, exceptionCase.Status);
        Assert.Equal(ExceptionSeverity.Medium, exceptionCase.Severity);
        Assert.Null(exceptionCase.AssignedToUserId);
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
                ExceptionSeverity.Medium,
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
                ExceptionSeverity.Medium,
                EvaluatedAtUtc.AddMinutes(1)));

        Assert.Throws<InvalidOperationException>(() =>
            ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                otherFixture.PurchaseOrderLine,
                fixture.MatchResult,
                ExceptionSeverity.Medium,
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
                ExceptionSeverity.Medium,
                EvaluatedAtUtc.AddTicks(-1)));

        var exceptionCase = CreateExceptionCase();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            exceptionCase.TransitionTo(
                ExceptionCaseStatus.Analyzing,
                exceptionCase.OpenedAtUtc.AddTicks(-1)));
    }

    [Fact]
    public void AssignmentRequiresAUserIdentifier()
    {
        var exceptionCase = CreateExceptionCase();
        var userId = Guid.NewGuid();

        exceptionCase.AssignTo(userId);

        Assert.Equal(userId, exceptionCase.AssignedToUserId);
        Assert.Throws<ArgumentException>(() => exceptionCase.AssignTo(Guid.Empty));
        Assert.Equal(userId, exceptionCase.AssignedToUserId);
    }

    [Fact]
    public void SeverityMustBeDefined()
    {
        var fixture = CreateFixture(failed: true);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                fixture.InvoiceLine,
                fixture.PurchaseOrderLine,
                fixture.MatchResult,
                (ExceptionSeverity)999,
                EvaluatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void FailedMatchCreatesFourDeterministicEvidenceRecords()
    {
        var fixture = CreateFixture(failed: true);
        var openedAt = EvaluatedAtUtc.AddMinutes(1);

        var exceptionCase = ExceptionCase.CreateFromFailedMatch(
            Guid.NewGuid(),
            fixture.InvoiceLine,
            fixture.PurchaseOrderLine,
            fixture.MatchResult,
            ExceptionSeverity.Medium,
            openedAt);

        Assert.Equal(4, exceptionCase.Evidences.Count);

        var invoicePriceEvidence = Assert.Single(
            exceptionCase.Evidences,
            e => e.Type == ExceptionEvidenceType.InvoicePrice);
        Assert.Equal(fixture.TenantId, invoicePriceEvidence.TenantId);
        Assert.Equal(exceptionCase.Id, invoicePriceEvidence.ExceptionCaseId);
        Assert.Equal(fixture.InvoiceLine.Id, invoicePriceEvidence.InvoiceLineId);
        Assert.Null(invoicePriceEvidence.PurchaseOrderLineId);
        Assert.Null(invoicePriceEvidence.MatchResultId);
        Assert.Null(invoicePriceEvidence.MatchingRuleId);
        Assert.Equal(openedAt, invoicePriceEvidence.RecordedAtUtc);

        var poPriceEvidence = Assert.Single(
            exceptionCase.Evidences,
            e => e.Type == ExceptionEvidenceType.PurchaseOrderPrice);
        Assert.Equal(fixture.TenantId, poPriceEvidence.TenantId);
        Assert.Equal(exceptionCase.Id, poPriceEvidence.ExceptionCaseId);
        Assert.Equal(fixture.PurchaseOrderLine.Id, poPriceEvidence.PurchaseOrderLineId);
        Assert.Null(poPriceEvidence.InvoiceLineId);
        Assert.Null(poPriceEvidence.MatchResultId);
        Assert.Null(poPriceEvidence.MatchingRuleId);
        Assert.Equal(openedAt, poPriceEvidence.RecordedAtUtc);

        var varianceEvidence = Assert.Single(
            exceptionCase.Evidences,
            e => e.Type == ExceptionEvidenceType.PriceVariance);
        Assert.Equal(fixture.TenantId, varianceEvidence.TenantId);
        Assert.Equal(exceptionCase.Id, varianceEvidence.ExceptionCaseId);
        Assert.Equal(fixture.MatchResult.Id, varianceEvidence.MatchResultId);
        Assert.Null(varianceEvidence.InvoiceLineId);
        Assert.Null(varianceEvidence.PurchaseOrderLineId);
        Assert.Null(varianceEvidence.MatchingRuleId);
        Assert.Equal(openedAt, varianceEvidence.RecordedAtUtc);

        var toleranceEvidence = Assert.Single(
            exceptionCase.Evidences,
            e => e.Type == ExceptionEvidenceType.RuleTolerance);
        Assert.Equal(fixture.TenantId, toleranceEvidence.TenantId);
        Assert.Equal(exceptionCase.Id, toleranceEvidence.ExceptionCaseId);
        Assert.Equal(fixture.MatchResult.MatchingRuleId, toleranceEvidence.MatchingRuleId);
        Assert.Null(toleranceEvidence.InvoiceLineId);
        Assert.Null(toleranceEvidence.PurchaseOrderLineId);
        Assert.Null(toleranceEvidence.MatchResultId);
        Assert.Equal(openedAt, toleranceEvidence.RecordedAtUtc);
    }

    [Theory]
    [InlineData(ExceptionCaseStatus.Open)]
    [InlineData(ExceptionCaseStatus.WaitingAction)]
    [InlineData(ExceptionCaseStatus.WaitingExternal)]
    public void OverrideCreatesResolutionAndAuditAndResolvesCase(ExceptionCaseStatus sourceStatus)
    {
        var exceptionCase = CreateExceptionCase();
        Transition(exceptionCase, ExceptionCaseStatus.Analyzing);
        Transition(exceptionCase, ExceptionCaseStatus.Open);
        if (sourceStatus is not ExceptionCaseStatus.Open)
        {
            Transition(exceptionCase, sourceStatus);
        }

        var actorUserId = Guid.NewGuid();
        var resolvedAt = exceptionCase.OpenedAtUtc.AddMinutes(5);
        var artifacts = exceptionCase.ApplyOverride(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorUserId,
            "  AP manager approved the documented variance.  ",
            resolvedAt);

        Assert.Equal(ExceptionCaseStatus.Resolved, exceptionCase.Status);
        Assert.Equal(resolvedAt, exceptionCase.ResolvedAtUtc);
        Assert.Equal(ResolutionType.Override, artifacts.Resolution.Type);
        Assert.Equal("AP manager approved the documented variance.", artifacts.Resolution.Reason);
        Assert.Equal(actorUserId, artifacts.Resolution.ActorUserId);
        Assert.Equal(sourceStatus, artifacts.Resolution.PreviousStatus);
        Assert.Equal(ExceptionCaseStatus.Resolved, artifacts.Resolution.NewStatus);
        Assert.Equal(AuditEventAction.ExceptionOverride, artifacts.AuditEvent.Action);
        Assert.Equal(artifacts.Resolution.Reason, artifacts.AuditEvent.Reason);
        Assert.Equal(sourceStatus, artifacts.AuditEvent.PreviousStatus);
        Assert.Equal(ExceptionCaseStatus.Resolved, artifacts.AuditEvent.NewStatus);
        Assert.Equal(resolvedAt, artifacts.AuditEvent.OccurredAtUtc);
    }

    [Fact]
    public void OverrideRejectsBlankReasonWithoutChangingState()
    {
        var exceptionCase = CreateOpenExceptionCase();

        Assert.Throws<ArgumentException>(() => exceptionCase.ApplyOverride(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   ",
            exceptionCase.OpenedAtUtc.AddMinutes(1)));

        Assert.Equal(ExceptionCaseStatus.Open, exceptionCase.Status);
        Assert.Null(exceptionCase.ResolvedAtUtc);
    }

    [Fact]
    public void OverrideRejectsInvalidStateAndSecondResolution()
    {
        var newCase = CreateExceptionCase();
        Assert.Throws<InvalidOperationException>(() => newCase.ApplyOverride(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Manual decision",
            newCase.OpenedAtUtc.AddMinutes(1)));

        var resolvedCase = CreateOpenExceptionCase();
        resolvedCase.ApplyOverride(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "First decision",
            resolvedCase.OpenedAtUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => resolvedCase.ApplyOverride(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Duplicate decision",
            resolvedCase.OpenedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void EvidenceFactoryValidatesRequiredIdentifiers()
    {
        var tenantId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreateInvoicePrice(Guid.Empty, tenantId, caseId, lineId, now));
        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreateInvoicePrice(Guid.NewGuid(), Guid.Empty, caseId, lineId, now));
        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreateInvoicePrice(Guid.NewGuid(), tenantId, Guid.Empty, lineId, now));
        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreateInvoicePrice(Guid.NewGuid(), tenantId, caseId, Guid.Empty, now));

        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreatePurchaseOrderPrice(Guid.NewGuid(), tenantId, caseId, Guid.Empty, now));

        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreatePriceVariance(Guid.NewGuid(), tenantId, caseId, Guid.Empty, now));

        Assert.Throws<ArgumentException>(() =>
            ExceptionCaseEvidence.CreateRuleTolerance(Guid.NewGuid(), tenantId, caseId, Guid.Empty, now));
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
            ExceptionSeverity.Medium,
            EvaluatedAtUtc.AddMinutes(1));
    }

    private static ExceptionCase CreateOpenExceptionCase()
    {
        var exceptionCase = CreateExceptionCase();
        Transition(exceptionCase, ExceptionCaseStatus.Analyzing);
        Transition(exceptionCase, ExceptionCaseStatus.Open);
        return exceptionCase;
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
