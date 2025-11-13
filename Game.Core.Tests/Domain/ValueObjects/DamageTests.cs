using Game.Core.Domain.ValueObjects;
using Xunit;

namespace Game.Core.Tests.Domain.ValueObjects;

public class DamageTests
{
    [Fact]
    public void EffectiveAmount_is_never_negative()
    {
        var d1 = new Damage(-10, DamageType.Physical);
        var d2 = new Damage(0, DamageType.Fire);
        var d3 = new Damage(5, DamageType.Ice, isCritical: true);
        Assert.Equal(0, d1.EffectiveAmount);
        Assert.Equal(0, d2.EffectiveAmount);
        Assert.Equal(5, d3.EffectiveAmount);
        Assert.True(d3.IsCritical);
    }
}

