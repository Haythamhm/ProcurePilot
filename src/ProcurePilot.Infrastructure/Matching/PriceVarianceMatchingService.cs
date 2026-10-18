using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Application.Matching;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Exceptions;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Infrastructure.Matching;

internal sealed class PriceVarianceMatchingService(
    ProcurePilotDbContext dbContext,
    ITenantContext tenantContext,
    TimeProvider timeProvider,
    IOptions<ExceptionManagementOptions> exceptionOptions) : IPriceVarianceMatchingService
{
    public async Task<PriceVarianceMatchOutcome> EvaluateAsync(
        PriceVarianceMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdentifier(command.InvoiceLineId, nameof(command.InvoiceLineId));
        ValidateIdentifier(command.PurchaseOrderLineId, nameof(command.PurchaseOrderLineId));
        ValidateIdentifier(command.MatchingRuleId, nameof(command.MatchingRuleId));

        tenantContext.RequireTenantId();

        var invoiceLine = await dbContext.InvoiceLines
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == command.InvoiceLineId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The invoice line was not found for the active tenant.");
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == invoiceLine.InvoiceId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The invoice was not found for the active tenant.");
        var purchaseOrderLine = await dbContext.PurchaseOrderLines
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == command.PurchaseOrderLineId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The purchase order line was not found for the active tenant.");
        var purchaseOrder = await dbContext.PurchaseOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == purchaseOrderLine.PurchaseOrderId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The purchase order was not found for the active tenant.");
        var matchingRule = await dbContext.MatchingRules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == command.MatchingRuleId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The matching rule was not found for the active tenant.");

        if (invoice.ProcessingStatus is not ProcurePilot.Domain.Invoices.InvoiceProcessingStatus.Ready)
        {
            throw new InvalidOperationException("Only a reviewed invoice can be matched.");
        }

        if (invoice.VendorId != purchaseOrder.VendorId)
        {
            throw new InvalidOperationException("The invoice and purchase order must belong to the same supplier.");
        }

        if (!string.Equals(invoice.Currency, purchaseOrder.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The invoice and purchase order currencies must match.");
        }

        var evaluatedAtUtc = timeProvider.GetUtcNow();
        var matchResult = MatchResult.EvaluatePriceVariance(
            Guid.NewGuid(),
            invoiceLine,
            purchaseOrderLine,
            matchingRule,
            evaluatedAtUtc);
        dbContext.MatchResults.Add(matchResult);

        ExceptionCase? exceptionCase = null;
        if (matchResult.Status is MatchResultStatus.Failed)
        {
            exceptionCase = ExceptionCase.CreateFromFailedMatch(
                Guid.NewGuid(),
                invoiceLine,
                purchaseOrderLine,
                matchResult,
                exceptionOptions.Value.DefaultPriceVarianceSeverity,
                evaluatedAtUtc);
            // The deterministic match and its evidence are complete: the case is ready
            // for the human work queue without an artificial operator transition.
            exceptionCase.TransitionTo(ExceptionCaseStatus.Analyzing, evaluatedAtUtc);
            exceptionCase.TransitionTo(ExceptionCaseStatus.Open, evaluatedAtUtc);
            dbContext.ExceptionCases.Add(exceptionCase);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PriceVarianceMatchOutcome(
            matchResult.Id,
            matchResult.Status,
            matchResult.ExceptionCode,
            exceptionCase?.Id);
    }

    private static void ValidateIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An identifier is required.", parameterName);
        }
    }
}
