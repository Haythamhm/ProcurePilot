using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Exceptions;

public sealed class AuditEvent : ITenantScopedEntity
{
    public const int ActionMaxLength = 64;

    private AuditEvent()
    {
    }

    internal AuditEvent(
        Guid id,
        Guid tenantId,
        Guid exceptionCaseId,
        Guid actorUserId,
        string reason,
        ExceptionCaseStatus previousStatus,
        DateTimeOffset occurredAtUtc)
    {
        Id = RequireIdentifier(id, nameof(id));
        TenantId = RequireIdentifier(tenantId, nameof(tenantId));
        ExceptionCaseId = RequireIdentifier(exceptionCaseId, nameof(exceptionCaseId));
        ActorUserId = RequireIdentifier(actorUserId, nameof(actorUserId));
        Reason = Resolution.IsValidReason(reason)
            ? reason.Trim()
            : throw new ArgumentException("A valid audit reason is required.", nameof(reason));
        PreviousStatus = previousStatus;
        NewStatus = ExceptionCaseStatus.Resolved;
        Action = AuditEventAction.ExceptionOverride;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ExceptionCaseId { get; private set; }

    public AuditEventAction Action { get; private set; }

    public Guid ActorUserId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public ExceptionCaseStatus PreviousStatus { get; private set; }

    public ExceptionCaseStatus NewStatus { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    private static Guid RequireIdentifier(Guid value, string parameterName) =>
        value != Guid.Empty
            ? value
            : throw new ArgumentException("An identifier is required.", parameterName);
}
