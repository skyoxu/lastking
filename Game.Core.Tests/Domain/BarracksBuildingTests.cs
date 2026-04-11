using FluentAssertions;
using Game.Core.Domain.Building;
using Xunit;

namespace Game.Core.Tests.Domain;

public class BarracksBuildingTests
{
    // ACC:T16.16
    [Fact]
    public void ShouldRepresentBarracksAsBuildingSubclass_WhenDomainContractsAreChecked()
    {
        typeof(BarracksBuilding).IsSubclassOf(typeof(Building)).Should().BeTrue();
        var barracks = new BarracksBuilding();
        barracks.Type.Should().Be(BuildingTypeIds.Barracks);
        barracks.FootprintSize.Should().Be(2);
        barracks.Hp.Should().BeGreaterThan(0);
    }
}
