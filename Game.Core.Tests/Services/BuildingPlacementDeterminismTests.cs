using System.Linq;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Engine.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BuildingPlacementDeterminismTests
{
    // ACC:T13.5
    [Fact]
    public void ShouldReplayIdenticalPlacementGateAndRejectionResults_WhenGridResourcesSeedAndInputsAreSame()
    {
        var loop = new BuildingSystemCoreLoop();
        var state = new BuildingPlacementState(width: 8, height: 8, resources: 300);
        state.Grid.Occupied.Add(new GridPoint(2, 2));
        state.Grid.Occupied.Add(new GridPoint(4, 4));
        var inputs = new[]
        {
            new BuildingCoreLoopInput(BuildingTypeIds.Wall, new GridPoint(1, 1)),
            new BuildingCoreLoopInput(BuildingTypeIds.Barracks, new GridPoint(2, 2)),
            new BuildingCoreLoopInput(BuildingTypeIds.MgTower, new GridPoint(5, 5)),
        };

        var first = loop.Run(state, seed: 2026, inputs);
        var second = loop.Run(state, seed: 2026, inputs);

        second.AcceptedPlacements.Should().BeEquivalentTo(first.AcceptedPlacements);
        second.Rejections.Should().BeEquivalentTo(first.Rejections);
        second.ResourcesRemaining.Should().Be(first.ResourcesRemaining);
        second.OccupiedSnapshot.Should().BeEquivalentTo(first.OccupiedSnapshot);
        second.GatePath.Should().Equal(first.GatePath);
    }

    // ACC:T13.14
    [Fact]
    public void ShouldKeepGridAndResourcesUnchanged_WhenBoundaryOrCollisionPlacementIsRejected()
    {
        var loop = new BuildingSystemCoreLoop();
        var state = new BuildingPlacementState(width: 5, height: 5, resources: 90);
        state.Grid.Occupied.Add(new GridPoint(1, 1));
        var beforeResources = state.Resources;
        var beforeOccupied = state.Grid.Occupied.ToArray();
        var inputs = new[]
        {
            new BuildingCoreLoopInput(BuildingTypeIds.Castle, new GridPoint(4, 4)),
            new BuildingCoreLoopInput(BuildingTypeIds.Wall, new GridPoint(1, 1)),
        };

        var result = loop.Run(state, seed: 13, inputs);

        result.Rejections.Should().HaveCount(2);
        state.Resources.Should().Be(beforeResources);
        state.Grid.Occupied.Should().BeEquivalentTo(beforeOccupied);
    }
}
