using FluentAssertions;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public class BuildingUpgradeRepairServiceTests
{
    // ACC:T15.25
    [Fact]
    public void ShouldEmitOrderedTimelineIncludingRefusalAndPostIdleCompletion_WhenConcurrentRequestIsRejectedThenRetriedAfterIdle()
    {
        var sut = CreateSut();

        var upgradeStarted = sut.TryStartUpgrade();
        var repairAcceptedDuringUpgrade = sut.TryStartRepair();

        sut.AdvanceTick();
        sut.AdvanceTick();
        sut.ApplyDamage(30);

        var repairStartedAfterIdle = sut.TryStartRepair();

        sut.AdvanceTick();
        sut.AdvanceTick();

        upgradeStarted.Should().BeTrue();
        repairAcceptedDuringUpgrade.Should().BeFalse();
        repairStartedAfterIdle.Should().BeTrue();

        sut.Timeline.Should().ContainInOrder(
            "upgrade:start",
            "repair:refused.concurrent",
            "upgrade:progress",
            "upgrade:completed",
            "repair:start",
            "repair:progress",
            "repair:completed");
    }

    [Fact]
    public void ShouldKeepActiveOperationUnchanged_WhenConcurrentRequestIsRefused()
    {
        var sut = CreateSut();
        sut.TryStartUpgrade();

        var stateBefore = sut.State;
        var elapsedBefore = sut.ElapsedTicks;

        var accepted = sut.TryStartRepair();

        accepted.Should().BeFalse();
        sut.State.Should().Be(stateBefore);
        sut.ElapsedTicks.Should().Be(elapsedBefore);
    }

    private static BuildingUpgradeRepairRuntime CreateSut()
    {
        return new BuildingUpgradeRepairRuntime(
            level: 1,
            maxLevel: 5,
            maxHp: 100,
            currentHp: 40,
            gold: 3000,
            buildCost: 1000,
            state: BuildingOperationState.Idle,
            upgradeTicksRequired: 2,
            repairTicksRequired: 2);
    }
}
