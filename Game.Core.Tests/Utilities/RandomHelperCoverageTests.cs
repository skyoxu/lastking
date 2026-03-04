using FluentAssertions;
using Game.Core.Utilities;
using Xunit;

namespace Game.Core.Tests.Utilities;

public class RandomHelperCoverageTests
{
    [Fact]
    public void ShouldRespectRange_WhenCallingNextInt()
    {
        var value = RandomHelper.NextInt(1, 3);
        value.Should().BeInRange(1, 2);
    }

    [Fact]
    public void ShouldBeWithinUnitInterval_WhenCallingNextDouble()
    {
        var value = RandomHelper.NextDouble();
        value.Should().BeGreaterThanOrEqualTo(0.0);
        value.Should().BeLessThan(1.0);
    }
}
