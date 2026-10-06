using ProcurePilot.Domain.Tenancy;

namespace ProcurePilot.Domain.Matching;

public sealed class MatchingRule : ITenantScopedEntity
{
    public const int RuleTypeMaxLength = 64;
    public const int ToleranceScale = 4;

    private MatchingRule()
    {
    }

    public MatchingRule(
        Guid id,
        Guid tenantId,
        MatchingRuleType type,
        decimal allowedTolerancePercent,
        DateTimeOffset effectiveFromUtc)
        : this(
            id,
            tenantId,
            type,
            allowedTolerancePercent,
            version: 1,
            effectiveFromUtc)
    {
    }

    private MatchingRule(
        Guid id,
        Guid tenantId,
        MatchingRuleType type,
        decimal allowedTolerancePercent,
        int version,
        DateTimeOffset effectiveFromUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A matching rule identifier is required.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(tenantId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "The matching rule type is invalid.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "The rule version must be positive.");
        }

        Id = id;
        TenantId = tenantId;
        Type = type;
        AllowedTolerancePercent = ValidateTolerance(allowedTolerancePercent);
        Version = version;
        EffectiveFromUtc = effectiveFromUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public MatchingRuleType Type { get; private set; }

    public decimal AllowedTolerancePercent { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public DateTimeOffset? EffectiveToUtc { get; private set; }

    public bool IsCurrent => EffectiveToUtc is null;

    public MatchingRule CreateNextVersion(
        Guid id,
        decimal allowedTolerancePercent,
        DateTimeOffset effectiveFromUtc)
    {
        if (!IsCurrent)
        {
            throw new InvalidOperationException("Only the current matching rule can be superseded.");
        }

        var nextEffectiveFromUtc = effectiveFromUtc.ToUniversalTime();
        if (nextEffectiveFromUtc <= EffectiveFromUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveFromUtc),
                "The next rule version must become effective after the current version.");
        }

        var nextVersion = checked(Version + 1);
        var nextRule = new MatchingRule(
            id,
            TenantId,
            Type,
            allowedTolerancePercent,
            nextVersion,
            nextEffectiveFromUtc);

        EffectiveToUtc = nextEffectiveFromUtc;
        return nextRule;
    }

    private static decimal ValidateTolerance(decimal tolerance)
    {
        if (tolerance < 0m || tolerance > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "The allowed tolerance must be between 0 and 100 percent.");
        }

        if (decimal.Round(tolerance, ToleranceScale) != tolerance)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                $"The allowed tolerance must have at most {ToleranceScale} decimal places.");
        }

        return tolerance;
    }
}
