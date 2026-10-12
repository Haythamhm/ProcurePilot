using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Domain.Exceptions;

namespace ProcurePilot.Infrastructure.Persistence.Queries;

internal sealed class ExceptionWorkspaceQueries(ProcurePilotDbContext dbContext)
    : IExceptionWorkspaceQueries
{
    public async Task<ExceptionWorkspaceReadModel?> GetAsync(
        Guid exceptionCaseId,
        CancellationToken cancellationToken = default)
    {
        if (exceptionCaseId == Guid.Empty)
        {
            return null;
        }

        var projection = await (
                from exceptionCase in dbContext.ExceptionCases.AsNoTracking()
                where exceptionCase.Id == exceptionCaseId
                join invoice in dbContext.Invoices.AsNoTracking()
                    on new { exceptionCase.TenantId, exceptionCase.InvoiceId }
                    equals new { invoice.TenantId, InvoiceId = invoice.Id }
                join matchResult in dbContext.MatchResults.AsNoTracking()
                    on new { exceptionCase.TenantId, exceptionCase.MatchResultId }
                    equals new { matchResult.TenantId, MatchResultId = matchResult.Id }
                join invoiceLine in dbContext.InvoiceLines.AsNoTracking()
                    on new { matchResult.TenantId, matchResult.InvoiceLineId }
                    equals new { invoiceLine.TenantId, InvoiceLineId = invoiceLine.Id }
                join purchaseOrder in dbContext.PurchaseOrders.AsNoTracking()
                    on new { exceptionCase.TenantId, exceptionCase.PurchaseOrderId }
                    equals new { purchaseOrder.TenantId, PurchaseOrderId = purchaseOrder.Id }
                join purchaseOrderLine in dbContext.PurchaseOrderLines.AsNoTracking()
                    on new { matchResult.TenantId, matchResult.PurchaseOrderLineId }
                    equals new { purchaseOrderLine.TenantId, PurchaseOrderLineId = purchaseOrderLine.Id }
                join matchingRule in dbContext.MatchingRules.AsNoTracking()
                    on new { matchResult.TenantId, matchResult.MatchingRuleId }
                    equals new { matchingRule.TenantId, MatchingRuleId = matchingRule.Id }
                join vendor in dbContext.Vendors.AsNoTracking()
                    on new { invoice.TenantId, invoice.VendorId }
                    equals new { vendor.TenantId, VendorId = (Guid?)vendor.Id }
                    into vendors
                from vendor in vendors.DefaultIfEmpty()
                join document in dbContext.InvoiceDocuments.AsNoTracking()
                    on new { invoice.TenantId, InvoiceId = invoice.Id }
                    equals new { document.TenantId, document.InvoiceId }
                    into documents
                from document in documents.DefaultIfEmpty()
                join assignedUser in dbContext.Users.AsNoTracking()
                    on new { exceptionCase.TenantId, UserId = exceptionCase.AssignedToUserId }
                    equals new { assignedUser.TenantId, UserId = (Guid?)assignedUser.Id }
                    into assignedUsers
                from assignedUser in assignedUsers.DefaultIfEmpty()
                select new
                {
                    exceptionCase.Id,
                    exceptionCase.Type,
                    exceptionCase.Status,
                    exceptionCase.Severity,
                    exceptionCase.OpenedAtUtc,
                    exceptionCase.ResolvedAtUtc,
                    AssignedToDisplayName = assignedUser == null ? null : assignedUser.DisplayName,
                    VendorName = vendor == null ? "Vendor unavailable" : vendor.Name,
                    InvoiceNumber = invoice.InvoiceNumber ?? "Invoice number unavailable",
                    InvoiceCurrency = invoice.Currency ?? purchaseOrder.Currency,
                    InvoiceLineNumber = invoiceLine.LineNumber,
                    InvoiceLineDescription = invoiceLine.Description,
                    InvoiceQuantity = invoiceLine.Quantity,
                    matchResult.InvoiceUnitPrice,
                    InvoiceSourceFileName = document == null ? null : document.OriginalFileName,
                    PurchaseOrderNumber = purchaseOrder.Number,
                    PurchaseOrderCurrency = purchaseOrder.Currency,
                    PurchaseOrderLineNumber = purchaseOrderLine.LineNumber,
                    PurchaseOrderLineDescription = purchaseOrderLine.Description,
                    PurchaseOrderQuantity = purchaseOrderLine.Quantity,
                    matchResult.PurchaseOrderUnitPrice,
                    matchResult.VariancePercent,
                    matchResult.AllowedTolerancePercent,
                    MatchingRuleType = matchingRule.Type,
                    matchResult.MatchingRuleVersion,
                    matchResult.EvaluatedAtUtc
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return null;
        }

        var rawEvidences = await (
                from evidence in dbContext.ExceptionCaseEvidences.AsNoTracking()
                where evidence.ExceptionCaseId == exceptionCaseId
                join invoiceLine in dbContext.InvoiceLines.AsNoTracking()
                    on new { evidence.TenantId, LineId = evidence.InvoiceLineId }
                    equals new { invoiceLine.TenantId, LineId = (Guid?)invoiceLine.Id }
                    into invoiceLines
                from invoiceLine in invoiceLines.DefaultIfEmpty()
                join poLine in dbContext.PurchaseOrderLines.AsNoTracking()
                    on new { evidence.TenantId, LineId = evidence.PurchaseOrderLineId }
                    equals new { poLine.TenantId, LineId = (Guid?)poLine.Id }
                    into poLines
                from poLine in poLines.DefaultIfEmpty()
                join matchResult in dbContext.MatchResults.AsNoTracking()
                    on new { evidence.TenantId, ResultId = evidence.MatchResultId }
                    equals new { matchResult.TenantId, ResultId = (Guid?)matchResult.Id }
                    into matchResults
                from matchResult in matchResults.DefaultIfEmpty()
                join matchingRule in dbContext.MatchingRules.AsNoTracking()
                    on new { evidence.TenantId, RuleId = evidence.MatchingRuleId }
                    equals new { matchingRule.TenantId, RuleId = (Guid?)matchingRule.Id }
                    into matchingRules
                from matchingRule in matchingRules.DefaultIfEmpty()
                select new
                {
                    evidence.Type,
                    evidence.RecordedAtUtc,
                    InvoiceUnitPrice = invoiceLine == null ? (decimal?)null : invoiceLine.UnitPrice,
                    InvoiceLineNumber = invoiceLine == null ? (int?)null : invoiceLine.LineNumber,
                    PurchaseOrderUnitPrice = poLine == null ? (decimal?)null : poLine.UnitPrice,
                    PurchaseOrderLineNumber = poLine == null ? (int?)null : poLine.LineNumber,
                    VariancePercent = matchResult == null ? (decimal?)null : matchResult.VariancePercent,
                    AllowedTolerancePercent = matchingRule == null ? (decimal?)null : matchingRule.AllowedTolerancePercent,
                    RuleType = matchingRule == null ? null : matchingRule.Type.ToString(),
                    RuleVersion = matchingRule == null ? (int?)null : matchingRule.Version
                })
            .ToListAsync(cancellationToken);

        var evidences = rawEvidences
            .OrderBy(e => (int)e.Type)
            .Select(e => MapEvidence(
                e.Type,
                e.RecordedAtUtc,
                e.InvoiceUnitPrice,
                e.InvoiceLineNumber,
                projection.InvoiceCurrency,
                e.PurchaseOrderUnitPrice,
                e.PurchaseOrderLineNumber,
                projection.PurchaseOrderCurrency,
                e.VariancePercent,
                e.AllowedTolerancePercent,
                e.RuleType,
                e.RuleVersion))
            .ToList();

        var rawAuditEvents = await (
                from auditEvent in dbContext.AuditEvents.AsNoTracking()
                where auditEvent.ExceptionCaseId == exceptionCaseId
                join actor in dbContext.Users.AsNoTracking()
                    on new { auditEvent.TenantId, UserId = auditEvent.ActorUserId }
                    equals new { actor.TenantId, UserId = actor.Id }
                orderby auditEvent.OccurredAtUtc descending, auditEvent.Id descending
                select new
                {
                    auditEvent.Action,
                    ActorDisplayName = actor.DisplayName,
                    auditEvent.Reason,
                    auditEvent.PreviousStatus,
                    auditEvent.NewStatus,
                    auditEvent.OccurredAtUtc
                })
            .ToListAsync(cancellationToken);

        var auditEvents = rawAuditEvents
            .Select(auditEvent => new ExceptionWorkspaceAuditEventReadModel(
                auditEvent.Action.ToString(),
                auditEvent.ActorDisplayName,
                auditEvent.Reason,
                auditEvent.PreviousStatus.ToString(),
                auditEvent.NewStatus.ToString(),
                auditEvent.OccurredAtUtc))
            .ToList();

        return new ExceptionWorkspaceReadModel(
            projection.Id,
            projection.Type.ToString(),
            projection.Status.ToString(),
            projection.Severity.ToString(),
            projection.OpenedAtUtc,
            projection.ResolvedAtUtc,
            projection.AssignedToDisplayName,
            projection.VendorName,
            projection.InvoiceNumber,
            projection.InvoiceCurrency,
            projection.InvoiceLineNumber,
            projection.InvoiceLineDescription,
            projection.InvoiceQuantity,
            projection.InvoiceUnitPrice,
            projection.InvoiceSourceFileName,
            projection.PurchaseOrderNumber,
            projection.PurchaseOrderCurrency,
            projection.PurchaseOrderLineNumber,
            projection.PurchaseOrderLineDescription,
            projection.PurchaseOrderQuantity,
            projection.PurchaseOrderUnitPrice,
            projection.VariancePercent,
            projection.AllowedTolerancePercent,
            projection.MatchingRuleType.ToString(),
            projection.MatchingRuleVersion,
            projection.EvaluatedAtUtc,
            evidences,
            auditEvents);
    }

    private static ExceptionWorkspaceEvidenceReadModel MapEvidence(
        ExceptionEvidenceType type,
        DateTimeOffset recordedAtUtc,
        decimal? invoiceUnitPrice,
        int? invoiceLineNumber,
        string invoiceCurrency,
        decimal? purchaseOrderUnitPrice,
        int? purchaseOrderLineNumber,
        string purchaseOrderCurrency,
        decimal? variancePercent,
        decimal? allowedTolerancePercent,
        string? ruleType,
        int? ruleVersion)
    {
        return type switch
        {
            ExceptionEvidenceType.InvoicePrice => new ExceptionWorkspaceEvidenceReadModel(
                type.ToString(),
                "Invoice unit price",
                invoiceLineNumber.HasValue ? $"Invoice line {invoiceLineNumber.Value}" : "Invoice line",
                invoiceUnitPrice.HasValue ? $"{invoiceCurrency} {invoiceUnitPrice.Value.ToString("N2", CultureInfo.InvariantCulture)}" : "Unavailable",
                recordedAtUtc),
            ExceptionEvidenceType.PurchaseOrderPrice => new ExceptionWorkspaceEvidenceReadModel(
                type.ToString(),
                "Purchase order unit price",
                purchaseOrderLineNumber.HasValue ? $"PO line {purchaseOrderLineNumber.Value}" : "PO line",
                purchaseOrderUnitPrice.HasValue ? $"{purchaseOrderCurrency} {purchaseOrderUnitPrice.Value.ToString("N2", CultureInfo.InvariantCulture)}" : "Unavailable",
                recordedAtUtc),
            ExceptionEvidenceType.PriceVariance => new ExceptionWorkspaceEvidenceReadModel(
                type.ToString(),
                "Price variance",
                "Evaluated price difference",
                variancePercent.HasValue ? $"{(variancePercent.Value > 0 ? "+" : string.Empty)}{variancePercent.Value.ToString("0.####", CultureInfo.InvariantCulture)}%" : "Unavailable",
                recordedAtUtc),
            ExceptionEvidenceType.RuleTolerance => new ExceptionWorkspaceEvidenceReadModel(
                type.ToString(),
                "Allowed tolerance threshold",
                ruleType is not null && ruleVersion.HasValue ? $"{ruleType} · V{ruleVersion.Value}" : "Matching rule",
                allowedTolerancePercent.HasValue ? $"±{allowedTolerancePercent.Value.ToString("0.####", CultureInfo.InvariantCulture)}%" : "Unavailable",
                recordedAtUtc),
            _ => new ExceptionWorkspaceEvidenceReadModel(
                type.ToString(),
                type.ToString(),
                "Evidence reference",
                "Recorded",
                recordedAtUtc)
        };
    }
}
