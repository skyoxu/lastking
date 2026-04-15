using FluentAssertions;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BuildingOperationStateMachineTests
{
    // ACC:T15.11
    [Fact]
    public void ShouldRejectRepairStart_WhenUpgradeIsActive()
    {
        var sut = CreateSut(level: 2, hp: 40, gold: 2_000);
        sut.TryStartUpgrade().Should().BeTrue();

        var repairStarted = sut.TryStartRepair();

        repairStarted.Should().BeFalse();
        sut.State.Should().Be(BuildingOperationState.Upgrading);
    }

    // ACC:T15.12
    [Fact]
    public void ShouldReturnToIdle_WhenProgressCompletes()
    {
        var sut = CreateSut(level: 2, hp: 40, gold: 2_000);
        sut.TryStartUpgrade().Should().BeTrue();

        sut.AdvanceTick();
        sut.AdvanceTick();

        sut.State.Should().Be(BuildingOperationState.Idle);
    }

    // ACC:T15.14
    [Theory]
    [InlineData("non-idle")]
    [InlineData("full-hp")]
    [InlineData("insufficient-gold")]
    public void ShouldRejectRepairStartAndKeepStateAndResourcesUnchanged_WhenPreconditionsFail(string scenario)
    {
        var state = scenario == "non-idle" ? BuildingOperationState.Upgrading : BuildingOperationState.Idle;
        var hp = scenario == "full-hp" ? 100 : 40;
        var gold = scenario == "insufficient-gold" ? 100 : 1_000;
        var sut = CreateSut(level: 2, hp: hp, gold: gold, state: state);
        var stateBefore = sut.State;
        var hpBefore = sut.CurrentHp;
        var goldBefore = sut.Gold;

        var started = sut.TryStartRepair();

        started.Should().BeFalse();
        sut.State.Should().Be(stateBefore);
        sut.CurrentHp.Should().Be(hpBefore);
        sut.Gold.Should().Be(goldBefore);
    }

    // ACC:T15.17
    [Fact]
    public void ShouldSetStateToUpgradingOnly_WhenUpgradeStartSucceeds()
    {
        var sut = CreateSut(level: 1, hp: 30, gold: 2_000);
        var levelBefore = sut.Level;
        var hpBefore = sut.CurrentHp;

        var started = sut.TryStartUpgrade();

        started.Should().BeTrue();
        sut.State.Should().Be(BuildingOperationState.Upgrading);
        sut.Level.Should().Be(levelBefore);
        sut.CurrentHp.Should().Be(hpBefore);
    }

    // ACC:T15.19
    [Fact]
    public void ShouldRefuseSwitchToOtherOperationUntilIdle_WhenOperationAlreadyRunning()
    {
        var sut = CreateSut(level: 2, hp: 40, gold: 2_000);
        sut.TryStartUpgrade().Should().BeTrue();

        var switched = sut.TryStartRepair();

        switched.Should().BeFalse();
        sut.State.Should().Be(BuildingOperationState.Upgrading);
    }

    // ACC:T15.21
    [Fact]
    public void ShouldExposeIdleUpgradingRepairingStates_WhenInspectingStateMachine()
    {
        ((int)BuildingOperationState.Idle).Should().Be(0);
        ((int)BuildingOperationState.Upgrading).Should().Be(1);
        ((int)BuildingOperationState.Repairing).Should().Be(2);
    }

    private static BuildingUpgradeRepairRuntime CreateSut(
        int level,
        int hp,
        int gold,
        BuildingOperationState state = BuildingOperationState.Idle)
    {
        return new BuildingUpgradeRepairRuntime(
            level: level,
            maxLevel: 5,
            maxHp: 100,
            currentHp: hp,
            gold: gold,
            buildCost: 1000,
            state: state,
            upgradeTicksRequired: 2,
            repairTicksRequired: 2);
    }
}
