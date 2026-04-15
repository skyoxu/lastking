using FluentAssertions;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.State;

public sealed class BuildingOperationTimelineTests
{
    // ACC:T15.25
    [Fact]
    [Trait("acceptance", "ACC:T15.25")]
    public void ShouldEmitOrderedTimeline_WhenConcurrentRequestIsRefusedAndRetriedAfterIdle()
    {
        var sut = CreateSut(requiredTicksPerOperation: 2);

        var upgradeStarted = sut.TryStartUpgrade("barracks-1");
        var repairAcceptedDuringUpgrade = sut.TryStartRepair("wall-1");

        sut.AdvanceTick();
        sut.AdvanceTick();
        sut.ApplyDamage(30);

        var repairStartedAfterIdle = sut.TryStartRepair("wall-1");

        sut.AdvanceTick();
        sut.AdvanceTick();

        upgradeStarted.Should().BeTrue();
        repairAcceptedDuringUpgrade.Should().BeFalse();
        repairStartedAfterIdle.Should().BeTrue();

        sut.Timeline.Should().ContainInOrder(
            "upgrade:start:barracks-1",
            "repair:refused.concurrent:wall-1",
            "upgrade:progress:barracks-1:1/2",
            "upgrade:completed:barracks-1",
            "repair:start:wall-1",
            "repair:progress:wall-1:1/2",
            "repair:completed:wall-1");
    }

    [Fact]
    public void ShouldKeepActiveOperationUnchanged_WhenConcurrentRequestIsRefused()
    {
        var sut = CreateSut(requiredTicksPerOperation: 2);
        sut.TryStartUpgrade("barracks-1");

        var stateBefore = sut.State;
        var elapsedTicksBefore = sut.ElapsedTicks;

        var accepted = sut.TryStartRepair("wall-1");

        accepted.Should().BeFalse();
        sut.State.Should().Be(stateBefore);
        sut.ElapsedTicks.Should().Be(elapsedTicksBefore);
    }

    private static BuildingUpgradeRepairRuntime CreateSut(int requiredTicksPerOperation)
    {
        return new BuildingUpgradeRepairRuntime(
            level: 1,
            maxLevel: 5,
            maxHp: 100,
            currentHp: 40,
            gold: 5000,
            buildCost: 1000,
            state: BuildingOperationState.Idle,
            upgradeTicksRequired: requiredTicksPerOperation,
            repairTicksRequired: requiredTicksPerOperation);
    }
}
