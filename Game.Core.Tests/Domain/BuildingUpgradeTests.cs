using FluentAssertions;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Domain;

public sealed class BuildingUpgradeTests
{
    // ACC:T15.4
    [Fact]
    public void ShouldStartUpgrade_WhenIdleLevelBelowCapAndCostAffordable()
    {
        var sut = CreateSut(level: 1, hp: 30, gold: 2_000);

        var started = sut.TryStartUpgrade();

        started.Should().BeTrue();
        sut.State.Should().Be(BuildingOperationState.Upgrading);
    }

    // ACC:T15.5
    [Fact]
    public void ShouldIncreaseExactlyOneLevel_WhenUpgradeCompletes()
    {
        var sut = CreateSut(level: 2, hp: 40, gold: 3_000);

        var started = sut.TryStartUpgrade();
        started.Should().BeTrue();

        sut.AdvanceTick();
        sut.AdvanceTick();

        sut.Level.Should().Be(3);
        sut.Level.Should().BeLessThanOrEqualTo(5);
    }

    // ACC:T15.6
    [Theory]
    [InlineData("busy")]
    [InlineData("insufficient-gold")]
    [InlineData("max-level")]
    public void ShouldRejectUpgradeStart_WhenPreconditionsAreNotMet(string scenario)
    {
        var state = scenario == "busy" ? BuildingOperationState.Repairing : BuildingOperationState.Idle;
        var level = scenario == "max-level" ? 5 : 2;
        var gold = scenario == "insufficient-gold" ? 0 : 3_000;
        var sut = CreateSut(level: level, hp: 35, gold: gold, state: state);
        var levelBefore = sut.Level;
        var hpBefore = sut.CurrentHp;
        var goldBefore = sut.Gold;
        var stateBefore = sut.State;

        var started = sut.TryStartUpgrade();

        started.Should().BeFalse();
        sut.Level.Should().Be(levelBefore);
        sut.CurrentHp.Should().Be(hpBefore);
        sut.Gold.Should().Be(goldBefore);
        sut.State.Should().Be(stateBefore);
    }

    // ACC:T15.7
    [Fact]
    public void ShouldRestoreHpToFull_WhenUpgradeCompletes()
    {
        var sut = CreateSut(level: 1, hp: 20, gold: 2_000);

        var started = sut.TryStartUpgrade();
        started.Should().BeTrue();

        sut.AdvanceTick();
        sut.AdvanceTick();

        sut.CurrentHp.Should().Be(sut.MaxHp);
    }

    // ACC:T15.8
    [Fact]
    public void ShouldUseUpdatedConfiguredUpgradeCost_WhenConfigurationChanges()
    {
        var sut = CreateSut(level: 1, hp: 50, gold: 1_000);
        sut.OverrideUpgradeCost(level: 1, cost: 333);
        var goldBefore = sut.Gold;

        var started = sut.TryStartUpgrade();

        started.Should().BeTrue();
        sut.Gold.Should().Be(goldBefore - 333);
    }

    // ACC:T15.17
    [Fact]
    public void ShouldNotChangeLevelOrRestoreHpInSameTick_WhenUpgradeStarts()
    {
        var sut = CreateSut(level: 1, hp: 35, gold: 2_000);
        var levelBefore = sut.Level;
        var hpBefore = sut.CurrentHp;

        var started = sut.TryStartUpgrade();

        started.Should().BeTrue();
        sut.State.Should().Be(BuildingOperationState.Upgrading);
        sut.Level.Should().Be(levelBefore);
        sut.CurrentHp.Should().Be(hpBefore);
    }

    // ACC:T15.20
    [Fact]
    public void ShouldExposeCallableUpgradeAndRepairOperations_WhenInvokedThroughDomain()
    {
        var sut = CreateSut(level: 1, hp: 30, gold: 2_000);

        var upgradeStarted = sut.TryStartUpgrade();
        sut.AdvanceTick();
        sut.AdvanceTick();
        sut.ApplyDamage(20);
        var repairStarted = sut.TryStartRepair();

        upgradeStarted.Should().BeTrue();
        repairStarted.Should().BeTrue();
    }

    // ACC:T15.21
    [Fact]
    public void ShouldExposeOperationalBuildStates_WhenEvaluatingTransitions()
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
