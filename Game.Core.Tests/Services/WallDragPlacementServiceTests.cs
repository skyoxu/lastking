using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WallDragPlacementServiceTests
{
    // ACC:T13.12
    [Fact]
    public void ShouldCreateContinuousWallSegments_WhenDragPathTraversesValidNeighborCells()
    {
        var service = new WallDragPlacementService(costPerSegment: 5);
        var state = new BuildingPlacementState(width: 8, height: 8, resources: 100);
        var dragPath = new[]
        {
            new GridPoint(1, 1),
            new GridPoint(2, 1),
            new GridPoint(3, 1),
            new GridPoint(4, 1),
        };

        var result = service.TryPlaceDragLine(dragPath, state);

        result.CreatedSegments.Should().HaveCount(dragPath.Length);
        result.CreatedSegments.Select(x => x.Cell).Should().Equal(dragPath);
        state.Grid.Occupied.Should().BeEquivalentTo(dragPath);
        state.Resources.Should().Be(100 - (dragPath.Length * 5));

        for (var index = 1; index < result.CreatedSegments.Count; index++)
        {
            var previous = result.CreatedSegments[index - 1].Cell;
            var current = result.CreatedSegments[index].Cell;
            var distance = Math.Abs(previous.X - current.X) + Math.Abs(previous.Y - current.Y);
            distance.Should().Be(1);
        }
    }

    // ACC:T13.18
    [Fact]
    public void ShouldDeductCostOnlyForCreatedSegments_WhenDragPathContainsInvalidCells()
    {
        var service = new WallDragPlacementService(costPerSegment: 5);
        var state = new BuildingPlacementState(width: 8, height: 8, resources: 100);
        state.Grid.Blocked.Add(new GridPoint(3, 2));
        state.Grid.Occupied.Add(new GridPoint(0, 0));
        var dragPath = new[]
        {
            new GridPoint(1, 1),
            new GridPoint(2, 1),
            new GridPoint(3, 1),
            new GridPoint(3, 2),
            new GridPoint(99, 99),
        };
        var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

        var result = service.TryPlaceDragLine(dragPath, state);

        result.CreatedSegments.Select(x => x.Cell).Should().Equal(new[]
        {
            new GridPoint(1, 1),
            new GridPoint(2, 1),
            new GridPoint(3, 1),
        });
        result.SkippedCells.Should().BeEquivalentTo(new[] { new GridPoint(3, 2), new GridPoint(99, 99) });
        state.Resources.Should().Be(100 - 15);
        result.TotalCharged.Should().Be(15);
        state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore.Union(result.CreatedSegments.Select(x => x.Cell)));
    }

    // ACC:T13.12
    [Fact]
    public void ShouldSkipNonAdjacentJumpCells_WhenDragPathContainsDiscontinuousSteps()
    {
        var service = new WallDragPlacementService(costPerSegment: 5);
        var state = new BuildingPlacementState(width: 8, height: 8, resources: 100);
        var dragPath = new[]
        {
            new GridPoint(1, 1),
            new GridPoint(2, 1),
            new GridPoint(4, 1),
            new GridPoint(5, 1),
        };

        var result = service.TryPlaceDragLine(dragPath, state);

        result.CreatedSegments.Select(x => x.Cell).Should().Equal(new[]
        {
            new GridPoint(1, 1),
            new GridPoint(2, 1),
        });
        result.SkippedCells.Should().Contain(new GridPoint(4, 1));
        result.SkippedCells.Should().Contain(new GridPoint(5, 1));
        result.TotalCharged.Should().Be(10);
        state.Resources.Should().Be(90);
    }
}
