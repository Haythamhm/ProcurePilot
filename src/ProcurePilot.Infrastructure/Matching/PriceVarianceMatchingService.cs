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
            .SingleOrDefaultAsync(
                value => value.Id == command.InvoiceLineId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The invoice line was not found for the active tenant.");
        var purchaseOrderLine = await dbContext.PurchaseOrderLines
            .SingleOrDefaultAsync(
                value => value.Id == command.PurchaseOrderLineId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The purchase order line was not found for the active tenant.");
        var matchingRule = await dbContext.MatchingRules
            .SingleOrDefaultAsync(
                value => value.Id == command.MatchingRuleId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The matching rule was not found for the active tenant.");

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
