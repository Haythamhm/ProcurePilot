using ProcurePilot.Domain.Matching;

namespace ProcurePilot.UnitTests;

public sealed class MatchingRuleTests
{
    [Fact]
    public void NewPriceVarianceRuleStartsAtVersionOne()
    {
        var effectiveFrom = new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.FromHours(1));

        var rule = new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            3.1250m,
            effectiveFrom);

        Assert.Equal(1, rule.Version);
        Assert.Equal(3.1250m, rule.AllowedTolerancePercent);
        Assert.Equal(TimeSpan.Zero, rule.EffectiveFromUtc.Offset);
        Assert.True(rule.IsCurrent);
        Assert.Null(rule.EffectiveToUtc);
    }

    [Fact]
    public void CreatingNextVersionClosesCurrentRuleWithoutMutatingItsTolerance()
    {
        var tenantId = Guid.NewGuid();
        var effectiveFrom = new DateTimeOffset(2026, 10, 7, 8, 0, 0, TimeSpan.Zero);
        var nextEffectiveFrom = effectiveFrom.AddDays(30);
        var current = new MatchingRule(
            Guid.NewGuid(),
            tenantId,
            MatchingRuleType.PriceVariance,
            3m,
            effectiveFrom);

        var next = current.CreateNextVersion(Guid.NewGuid(), 2.5m, nextEffectiveFrom);

        Assert.False(current.IsCurrent);
        Assert.Equal(nextEffectiveFrom, current.EffectiveToUtc);
        Assert.Equal(3m, current.AllowedTolerancePercent);
        Assert.Equal(2, next.Version);
        Assert.Equal(tenantId, next.TenantId);
        Assert.Equal(MatchingRuleType.PriceVariance, next.Type);
        Assert.Equal(2.5m, next.AllowedTolerancePercent);
        Assert.Equal(nextEffectiveFrom, next.EffectiveFromUtc);
        Assert.True(next.IsCurrent);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(100.0001)]
    [InlineData(3.12345)]
    public void RuleRejectsInvalidTolerance(double tolerance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            Convert.ToDecimal(tolerance),
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void RuleAcceptsInclusiveToleranceBounds(double tolerance)
    {
        var rule = new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            Convert.ToDecimal(tolerance),
            DateTimeOffset.UtcNow);

        Assert.Equal(Convert.ToDecimal(tolerance), rule.AllowedTolerancePercent);
    }

    [Fact]
    public void RuleRejectsInvalidIdentityAndType()
    {
        Assert.Throws<ArgumentException>(() => new MatchingRule(
            Guid.Empty,
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            3m,
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new MatchingRule(
            Guid.NewGuid(),
            Guid.Empty,
            MatchingRuleType.PriceVariance,
            3m,
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (MatchingRuleType)999,
            3m,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SupersededRuleCannotCreateAnotherVersion()
    {
        var current = new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            3m,
            DateTimeOffset.UtcNow);
        var next = current.CreateNextVersion(
            Guid.NewGuid(),
            2m,
            current.EffectiveFromUtc.AddDays(1));

        Assert.Throws<InvalidOperationException>(() => current.CreateNextVersion(
            Guid.NewGuid(),
            1m,
            next.EffectiveFromUtc.AddDays(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextVersionMustStartAfterCurrentVersion(int offsetMinutes)
    {
        var current = new MatchingRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MatchingRuleType.PriceVariance,
            3m,
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => current.CreateNextVersion(
            Guid.NewGuid(),
            2m,
            current.EffectiveFromUtc.AddMinutes(offsetMinutes)));
    }
}
