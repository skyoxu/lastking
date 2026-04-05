using System.Linq;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Engine.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Engine;

public sealed class BuildingSystemCoreLoopDeterminismTests
{
    // ACC:T13.5
    [Fact]
    public void ShouldProduceIdenticalPlacementAndGatePathingOutcomes_WhenStateSeedAndInputsAreReplayed()
    {
        var loop = new BuildingSystemCoreLoop();
        var state = new BuildingPlacementState(width: 6, height: 6, resources: 200);
        state.Grid.Occupied.Add(new GridPoint(1, 1));
        var inputs = new[]
        {
            new BuildingCoreLoopInput(BuildingTypeIds.Wall, new GridPoint(2, 1)),
            new BuildingCoreLoopInput(BuildingTypeIds.MgTower, new GridPoint(3, 1)),
            new BuildingCoreLoopInput(BuildingTypeIds.Barracks, new GridPoint(4, 4)),
        };

        var first = loop.Run(state, seed: 1337, inputs);
        var second = loop.Run(state, seed: 1337, inputs);

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public void ShouldRefuseInvalidPlacementsWithoutChangingInputState_WhenRejectedSequenceIsReplayed()
    {
        var loop = new BuildingSystemCoreLoop();
        var state = new BuildingPlacementState(width: 3, height: 3, resources: 0);
        state.Grid.Occupied.Add(new GridPoint(1, 1));
        var beforeResources = state.Resources;
        var beforeOccupied = state.Grid.Occupied.ToArray();
        var inputs = new[]
        {
            new BuildingCoreLoopInput(BuildingTypeIds.Wall, new GridPoint(1, 1)),
            new BuildingCoreLoopInput(BuildingTypeIds.Castle, new GridPoint(5, 5)),
            new BuildingCoreLoopInput(BuildingTypeIds.MgTower, new GridPoint(0, 0)),
        };

        var first = loop.Run(state, seed: 99, inputs);
        var second = loop.Run(state, seed: 99, inputs);

        first.Rejections.Should().BeEquivalentTo(second.Rejections);
        state.Resources.Should().Be(beforeResources);
        state.Grid.Occupied.Should().BeEquivalentTo(beforeOccupied);
    }
}
