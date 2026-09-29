namespace ProcurePilot.Web.Identity;

public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "Identity:Bootstrap";

    public string? TenantName { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }

    public string? DisplayName { get; init; }
}
