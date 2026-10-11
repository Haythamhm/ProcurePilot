using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Exceptions;

public sealed class Resolution : ITenantScopedEntity
{
    public const int TypeMaxLength = 32;
    public const int ReasonMaxLength = 1000;

    private Resolution()
    {
    }

    internal Resolution(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        Guid actorUserId,
        string reason,
        ExceptionCaseStatus previousStatus,
        DateTimeOffset resolvedAtUtc)
    {
        Id = RequireIdentifier(id, nameof(id));
        TenantId = RequireIdentifier(tenantId, nameof(tenantId));
        ExceptionCaseId = RequireIdentifier(exceptionCaseId, nameof(exceptionCaseId));
        ActorUserId = RequireIdentifier(actorUserId, nameof(actorUserId));
        Reason = NormalizeReason(reason);
        PreviousStatus = previousStatus;
        NewStatus = ExceptionCaseStatus.Resolved;
        Type = ResolutionType.Override;
        ResolvedAtUtc = resolvedAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ExceptionCaseId { get; private set; }

    public ResolutionType Type { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public Guid ActorUserId { get; private set; }

    public ExceptionCaseStatus PreviousStatus { get; private set; }

    public ExceptionCaseStatus NewStatus { get; private set; }

    public DateTimeOffset ResolvedAtUtc { get; private set; }

    public static bool IsValidReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length <= ReasonMaxLength;

    private static string NormalizeReason(string? reason)
    {
        if (!IsValidReason(reason))
        {
            throw new ArgumentException(
                $"An override reason between 1 and {ReasonMaxLength} characters is required.",
                nameof(reason));
        }

        return reason!.Trim();
    }

    private static Guid RequireIdentifier(Guid value, string parameterName) =>
        value != Guid.Empty
            ? value
            : throw new ArgumentException("An identifier is required.", parameterName);
}
