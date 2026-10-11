using ProcurePilot.Application.Security;

namespace ProcurePilot.Infrastructure.Identity;

internal sealed class NullCurrentUserContext : ICurrentUserContext
{
    private static readonly CurrentUserSnapshot Anonymous = new(
        null,
        false,
        new HashSet<string>(StringComparer.Ordinal));

    public Task<CurrentUserSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Anonymous);
}
