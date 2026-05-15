using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RuntimePressureStateMapperTests
{
    [Theory]
    [InlineData(90, "stable")]
    [InlineData(61, "stable")]
    [InlineData(60, "warning")]
    [InlineData(55, "warning")]
    [InlineData(41, "warning")]
    [InlineData(40, "danger")]
    [InlineData(35, "danger")]
    [InlineData(21, "danger")]
    [InlineData(20, "critical")]
    [InlineData(15, "critical")]
    [InlineData(0, "critical")]
    public void ShouldMapCastleHpIntoExactlyFourPressureStates(int hp, string expected)
    {
        var mapper = new RuntimePressureStateMapper();

        mapper.MapCastleHp(hp).Should().Be(expected);
    }
}
