using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class GatePathingRulesTests
{
    // ACC:T13.12
    [Fact]
    public void ShouldRejectMove_WhenEnteringNonGateWallCell()
    {
        var grid = PathingGrid.Create(5, 5)
            .WithWall(new GridPoint(1, 0));
        var engine = new GatePathingRulesEngine();

        var result = engine.TryMove(grid, new GridPoint(0, 0), MoveDirection.Right);

        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("WallBlocked");
        result.NewCell.Should().Be(new GridPoint(0, 0));
    }

    [Fact]
    public void ShouldAllowMove_WhenEnteringGateFromConfiguredDirection()
    {
        var grid = PathingGrid.Create(5, 5)
            .WithGate(new GridPoint(1, 0), MoveDirection.Right);
        var engine = new GatePathingRulesEngine();

        var result = engine.TryMove(grid, new GridPoint(0, 0), MoveDirection.Right);

        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Be("GateAllowed");
        result.NewCell.Should().Be(new GridPoint(1, 0));
    }

    // ACC:T13.14
    [Fact]
    public void ShouldRejectMove_WhenEnteringGateFromDisallowedDirection()
    {
        var grid = PathingGrid.Create(5, 5)
            .WithGate(new GridPoint(1, 0), MoveDirection.Right);
        var engine = new GatePathingRulesEngine();

        var result = engine.TryMove(grid, new GridPoint(2, 0), MoveDirection.Left);

        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("GateDirectionBlocked");
        result.NewCell.Should().Be(new GridPoint(2, 0));
    }

    // ACC:T13.13
    [Fact]
    public void ShouldSelectSameFallbackGate_WhenBlockedPathFallbackHasTie()
    {
        var grid = PathingGrid.Create(6, 6)
            .WithWall(new GridPoint(2, 1))
            .WithGate(new GridPoint(3, 2), MoveDirection.Right)
            .WithGate(new GridPoint(2, 3), MoveDirection.Down);
        var engine = new GatePathingRulesEngine();
        var fallbackDirections = new[] { MoveDirection.Right, MoveDirection.Down };

        var first = engine.TryMoveWithFallback(grid, new GridPoint(2, 2), MoveDirection.Up, fallbackDirections);
        var second = engine.TryMoveWithFallback(grid, new GridPoint(2, 2), MoveDirection.Up, fallbackDirections);

        first.IsAllowed.Should().BeTrue();
        second.IsAllowed.Should().BeTrue();
        second.NewCell.Should().Be(first.NewCell);
    }
}
