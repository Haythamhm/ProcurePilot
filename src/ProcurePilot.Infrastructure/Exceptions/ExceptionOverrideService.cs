using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Exceptions;
using ProcurePilot.Application.Security;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Exceptions;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Infrastructure.Exceptions;

internal sealed class ExceptionOverrideService(
    ProcurePilotDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext,
    TimeProvider timeProvider)
    : IExceptionOverrideService
{
    public async Task<ExceptionOverrideResult> OverrideAsync(
        ExceptionOverrideCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUser = await currentUserContext.GetAsync(cancellationToken);
        if (!currentUser.IsAuthenticated ||
            currentUser.UserId is not { } actorUserId ||
            actorUserId == Guid.Empty ||
            !currentUser.HasPermission(ProcurePilotPermissions.ExceptionOverride))
        {
            return new ExceptionOverrideResult(ExceptionOverrideOutcome.Forbidden);
        }

        if (!Resolution.IsValidReason(command.Reason))
        {
            return new ExceptionOverrideResult(ExceptionOverrideOutcome.InvalidReason);
        }

        var tenantId = tenantContext.RequireTenantId();
        var actorBelongsToTenant = await dbContext.Users
            .AnyAsync(
                user => user.Id == actorUserId && user.TenantId == tenantId,
                cancellationToken);
        if (!actorBelongsToTenant)
        {
            return new ExceptionOverrideResult(ExceptionOverrideOutcome.Forbidden);
        }

        var exceptionCase = await dbContext.ExceptionCases
            .SingleOrDefaultAsync(
                value => value.Id == command.ExceptionCaseId,
                cancellationToken);
        if (exceptionCase is null)
        {
            return new ExceptionOverrideResult(ExceptionOverrideOutcome.NotFound);
        }

        ExceptionOverrideArtifacts artifacts;
        try
        {
            artifacts = exceptionCase.ApplyOverride(
                Guid.NewGuid(),
                Guid.NewGuid(),
                actorUserId,
                command.Reason!,
                timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return new ExceptionOverrideResult(ExceptionOverrideOutcome.InvalidState);
        }

        dbContext.Resolutions.Add(artifacts.Resolution);
        dbContext.AuditEvents.Add(artifacts.AuditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ExceptionOverrideResult(
            ExceptionOverrideOutcome.Succeeded,
            artifacts.Resolution.Id,
            artifacts.Resolution.ResolvedAtUtc);
    }
}
