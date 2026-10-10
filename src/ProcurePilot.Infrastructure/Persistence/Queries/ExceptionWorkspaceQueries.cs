using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Exceptions;

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

        return projection is null
            ? null
            : new ExceptionWorkspaceReadModel(
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
                projection.EvaluatedAtUtc);
    }
}
