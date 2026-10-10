namespace ProcurePilot.Application.Exceptions;

public interface IExceptionWorkQueueQueries
{
    Task<IReadOnlyList<ExceptionWorkQueueItem>> ListAsync(
        ExceptionWorkQueueFilter filter,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default);
}

public enum ExceptionWorkQueueFilter
{
    Open = 1,
    Resolved = 2,
    AssignedToMe = 3
}

public sealed record ExceptionWorkQueueItem(
    Guid Id,
    string VendorName,
    string Type,
    string Status,
    string Severity,
    Guid? AssignedToUserId,
    DateTimeOffset OpenedAtUtc);
