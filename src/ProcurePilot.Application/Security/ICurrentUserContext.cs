namespace ProcurePilot.Application.Security;

public interface ICurrentUserContext
{
    Task<CurrentUserSnapshot> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record CurrentUserSnapshot(
    Guid? UserId,
    bool IsAuthenticated,
    IReadOnlySet<string> Permissions)
{
    public bool HasPermission(string permission) =>
        Permissions.Contains(permission);
}
