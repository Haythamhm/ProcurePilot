using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ProcurePilot.Application.Security;

namespace ProcurePilot.Web.Identity;

public sealed class ClaimsCurrentUserContext(
    AuthenticationStateProvider authenticationStateProvider)
    : ICurrentUserContext
{
    public async Task<CurrentUserSnapshot> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var principal = authenticationState.User;
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdValue, out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;
        var permissions = principal.FindAll(ProcurePilotPermissions.ClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        return new CurrentUserSnapshot(
            userId,
            principal.Identity?.IsAuthenticated is true,
            permissions);
    }
}
