namespace ProcurePilot.Application.Exceptions;

public interface IExceptionWorkQueueQueries
{
    Task<IReadOnlyList<ExceptionWorkQueueItem>> ListOpenAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ExceptionWorkQueueItem(
    Guid Id,
    string VendorName,
    string Type,
    string Status,
    DateTimeOffset OpenedAtUtc);
