using ProcurePilot.Domain.Invoices;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Domain.Purchasing;
using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Exceptions;

public sealed class ExceptionCase : ITenantScopedEntity
{
    public const int TypeMaxLength = 64;
    public const int StatusMaxLength = 32;
    public const int SeverityMaxLength = 16;

    private static readonly IReadOnlyDictionary<ExceptionCaseStatus, ExceptionCaseStatus[]> AllowedTransitions =
        new Dictionary<ExceptionCaseStatus, ExceptionCaseStatus[]>
        {
            [ExceptionCaseStatus.New] = [ExceptionCaseStatus.Analyzing],
            [ExceptionCaseStatus.Analyzing] = [ExceptionCaseStatus.Open],
            [ExceptionCaseStatus.Open] =
            [
                ExceptionCaseStatus.WaitingAction,
                ExceptionCaseStatus.WaitingExternal,
                ExceptionCaseStatus.Resolved
            ],
            [ExceptionCaseStatus.WaitingAction] =
            [
                ExceptionCaseStatus.Open,
                ExceptionCaseStatus.Resolved
            ],
            [ExceptionCaseStatus.WaitingExternal] =
            [
                ExceptionCaseStatus.Open,
                ExceptionCaseStatus.Resolved
            ],
            [ExceptionCaseStatus.Resolved] = [ExceptionCaseStatus.Closed],
            [ExceptionCaseStatus.Closed] = []
        };

    private ExceptionCase()
    {
    }

    private ExceptionCase(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        Guid purchaseOrderId,
        Guid matchResultId,
        MatchExceptionCode type,
        ExceptionSeverity severity,
        DateTimeOffset openedAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        PurchaseOrderId = purchaseOrderId;
        MatchResultId = matchResultId;
        Type = type;
        Severity = severity;
        Status = ExceptionCaseStatus.New;
        OpenedAtUtc = openedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public Guid PurchaseOrderId { get; private set; }

    public Guid MatchResultId { get; private set; }

    public MatchExceptionCode Type { get; private set; }

    public ExceptionCaseStatus Status { get; private set; }

    public ExceptionSeverity Severity { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public DateTimeOffset OpenedAtUtc { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private readonly List<ExceptionCaseEvidence> _evidences = [];

    public IReadOnlyCollection<ExceptionCaseEvidence> Evidences => _evidences.AsReadOnly();

    private readonly List<AiAnalysis> _analyses = [];

    public IReadOnlyCollection<AiAnalysis> Analyses => _analyses.AsReadOnly();

    public void AddAnalysis(AiAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        if (analysis.TenantId != TenantId || analysis.ExceptionCaseId != Id)
        {
            throw new InvalidOperationException("The analysis does not belong to this exception case.");
        }

        _analyses.Add(analysis);
    }

    public static ExceptionCase CreateFromFailedMatch(
        Guid id,
        InvoiceLine invoiceLine,
        PurchaseOrderLine purchaseOrderLine,
        MatchResult matchResult,
        ExceptionSeverity severity,
        DateTimeOffset openedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An exception case identifier is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(invoiceLine);
        ArgumentNullException.ThrowIfNull(purchaseOrderLine);
        ArgumentNullException.ThrowIfNull(matchResult);

        if (invoiceLine.TenantId != purchaseOrderLine.TenantId ||
            invoiceLine.TenantId != matchResult.TenantId)
        {
            throw new InvalidOperationException(
                "Exception case inputs must belong to the same tenant.");
        }

        if (invoiceLine.Id != matchResult.InvoiceLineId ||
            purchaseOrderLine.Id != matchResult.PurchaseOrderLineId)
        {
            throw new InvalidOperationException(
                "Exception case lines must match the evaluated match result.");
        }

        if (matchResult.Status is not MatchResultStatus.Failed ||
            matchResult.ExceptionCode is null)
        {
            throw new InvalidOperationException(
                "Only a failed match result with an exception code can create an exception case.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        var openedAt = openedAtUtc.ToUniversalTime();
        if (openedAt < matchResult.EvaluatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(openedAtUtc),
                "An exception case cannot open before its match result was evaluated.");
        }

        var exceptionCase = new ExceptionCase(
            id,
            matchResult.TenantId,
            invoiceLine.InvoiceId,
            purchaseOrderLine.PurchaseOrderId,
            matchResult.Id,
            matchResult.ExceptionCode.Value,
            severity,
            openedAt);

        exceptionCase._evidences.Add(ExceptionCaseEvidence.CreateInvoicePrice(
            Guid.NewGuid(),
            matchResult.TenantId,
            id,
            invoiceLine.Id,
            openedAt));

        exceptionCase._evidences.Add(ExceptionCaseEvidence.CreatePurchaseOrderPrice(
            Guid.NewGuid(),
            matchResult.TenantId,
            id,
            purchaseOrderLine.Id,
            openedAt));

        exceptionCase._evidences.Add(ExceptionCaseEvidence.CreatePriceVariance(
            Guid.NewGuid(),
            matchResult.TenantId,
            id,
            matchResult.Id,
            openedAt));

        exceptionCase._evidences.Add(ExceptionCaseEvidence.CreateRuleTolerance(
            Guid.NewGuid(),
            matchResult.TenantId,
            id,
            matchResult.MatchingRuleId,
            openedAt));

        return exceptionCase;
    }

    public void AssignTo(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("An assigned user identifier is required.", nameof(userId));
        }

        AssignedToUserId = userId;
    }

    public void TransitionTo(ExceptionCaseStatus nextStatus, DateTimeOffset occurredAtUtc)
    {
        if (!Enum.IsDefined(nextStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(nextStatus));
        }

        if (!AllowedTransitions[Status].Contains(nextStatus))
        {
            throw new InvalidOperationException(
                $"Exception case cannot transition from {Status} to {nextStatus}.");
        }

        var occurredAt = occurredAtUtc.ToUniversalTime();
        if (occurredAt < OpenedAtUtc ||
            (ResolvedAtUtc.HasValue && occurredAt < ResolvedAtUtc.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAtUtc),
                "A state transition cannot predate the exception case lifecycle.");
        }

        Status = nextStatus;
        if (nextStatus is ExceptionCaseStatus.Resolved)
        {
            ResolvedAtUtc = occurredAt;
        }
    }
}
