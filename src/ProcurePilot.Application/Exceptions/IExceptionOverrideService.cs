namespace ProcurePilot.Application.Exceptions;

public interface IExceptionOverrideService
{
    Task<ExceptionOverrideResult> OverrideAsync(
        ExceptionOverrideCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record ExceptionOverrideCommand(Guid ExceptionCaseId, string? Reason);

public sealed record ExceptionOverrideResult(
    ExceptionOverrideOutcome Outcome,
    Guid? ResolutionId = null,
    DateTimeOffset? ResolvedAtUtc = null);

public enum ExceptionOverrideOutcome
{
    Succeeded,
    Forbidden,
    InvalidReason,
    InvalidState,
    NotFound
}
