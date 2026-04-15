using FluentAssertions;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public class BuildingRepairProgressTests
{
    // ACC:T15.10
    [Fact]
    public void ShouldConsumeExactlyHalfBuildCost_WhenRepairStartsFromDamagedHpToFull()
    {
        var sut = CreateSut(buildCost: 1000, maxHp: 100, currentHp: 25, gold: 2000);

        var started = sut.TryStartRepair();

        started.Should().BeTrue();
        sut.TotalGoldDeducted.Should().Be(500);
        sut.Gold.Should().Be(1500);
    }

    // ACC:T15.12
    [Fact]
    public void ShouldApplyRepairIncrementally_WhenRepairHasJustStarted()
    {
        var sut = CreateSut(buildCost: 1000, maxHp: 100, currentHp: 40, gold: 2000);

        var started = sut.TryStartRepair();

        started.Should().BeTrue();
        sut.CurrentHp.Should().BeLessThan(sut.MaxHp, "repair must not be an instant full-heal action");
        sut.State.Should().Be(BuildingOperationState.Repairing);
    }

    // ACC:T15.13
    [Fact]
    public void ShouldReturnToIdleOnlyAfterProgressCompletes_WhenRepairRunsAcrossTicks()
    {
        var sut = CreateSut(buildCost: 1200, maxHp: 100, currentHp: 20, gold: 5000, repairTicksRequired: 3);

        var started = sut.TryStartRepair();

        started.Should().BeTrue();
        sut.State.Should().Be(BuildingOperationState.Repairing);

        sut.AdvanceRepairTick();
        sut.AdvanceRepairTick();
        sut.AdvanceRepairTick();

        sut.CurrentHp.Should().Be(sut.MaxHp);
        sut.State.Should().Be(BuildingOperationState.Idle);
    }

    // ACC:T15.14
    [Fact]
    public void ShouldExposeCompletionEffectsOnlyAfterNextTick_WhenRepairStartSucceeds()
    {
        var sut = CreateSut(buildCost: 900, maxHp: 100, currentHp: 70, gold: 5000);

        var started = sut.TryStartRepair();

        started.Should().BeTrue();
        sut.RepairCompletedEvents.Should().Be(0);

        sut.AdvanceRepairTick();

        sut.RepairCompletedEvents.Should().Be(1);
    }

    // ACC:T15.15
    [Theory]
    [InlineData("not-idle")]
    [InlineData("full-hp")]
    [InlineData("insufficient-gold")]
    public void ShouldRejectRepairStartAndKeepStateGoldAndHpUnchanged_WhenPreconditionsAreNotMet(string scenario)
    {
        var initialState = scenario == "not-idle" ? BuildingOperationState.Upgrading : BuildingOperationState.Idle;
        var initialHp = scenario == "full-hp" ? 100 : 40;
        var initialGold = scenario == "insufficient-gold" ? 100 : 2000;
        var sut = CreateSut(buildCost: 1000, maxHp: 100, currentHp: initialHp, gold: initialGold, state: initialState);
        var hpBefore = sut.CurrentHp;
        var goldBefore = sut.Gold;
        var stateBefore = sut.State;

        var started = sut.TryStartRepair();

        started.Should().BeFalse();
        sut.CurrentHp.Should().Be(hpBefore);
        sut.Gold.Should().Be(goldBefore);
        sut.State.Should().Be(stateBefore);
    }

    // ACC:T15.18
    [Theory]
    [InlineData(1)]
    [InlineData(37)]
    [InlineData(99)]
    public void ShouldChargeExactlyHalfBuildCostForAnyDamagedStartHp_WhenRepairReachesFullHp(int startingHp)
    {
        var buildCost = 2000;
        var sut = CreateSut(buildCost: buildCost, maxHp: 100, currentHp: startingHp, gold: 10000);

        var started = sut.TryStartRepair();

        started.Should().BeTrue();
        sut.AdvanceRepairTick();
        sut.AdvanceRepairTick();

        var expectedTotalRepairCost = buildCost / 2;
        sut.TotalGoldDeducted.Should().Be(expectedTotalRepairCost);
    }

    // ACC:T15.20
    [Fact]
    public void ShouldAdvanceRepairInMultipleCappedSteps_WhenProgressTicksAreApplied()
    {
        var buildCost = 1000;
        var sut = CreateSut(buildCost: buildCost, maxHp: 100, currentHp: 10, gold: 5000);

        var started = sut.TryStartRepair();

        started.Should().BeTrue();
        var hpAfterStart = sut.CurrentHp;
        var goldAfterStart = sut.Gold;

        sut.AdvanceRepairTick();
        var hpAfterTick1 = sut.CurrentHp;
        var goldAfterTick1 = sut.Gold;

        sut.AdvanceRepairTick();
        var hpAfterTick2 = sut.CurrentHp;
        var goldAfterTick2 = sut.Gold;

        hpAfterStart.Should().Be(10, "start should only schedule repair, not complete it");
        hpAfterTick1.Should().BeGreaterThan(hpAfterStart);
        hpAfterTick1.Should().BeLessThan(sut.MaxHp);
        hpAfterTick2.Should().BeGreaterThanOrEqualTo(hpAfterTick1);

        var step1Spent = goldAfterStart - goldAfterTick1;
        var step2Spent = goldAfterTick1 - goldAfterTick2;
        var expectedTotalRepairCost = buildCost / 2;
        var remainingAfterStep1 = expectedTotalRepairCost - step1Spent;

        step2Spent.Should().BeLessThanOrEqualTo(remainingAfterStep1);
    }

    // ACC:T15.9
    [Fact]
    public void ShouldExposeCallableUpgradeAndRepairOperations_WhenInvokedThroughService()
    {
        var sut = CreateSut(buildCost: 1000, maxHp: 100, currentHp: 60, gold: 5000);

        var upgradeStarted = sut.TryStartUpgrade();
        upgradeStarted.Should().BeTrue();
        sut.AdvanceTick();
        sut.AdvanceTick();
        sut.ApplyDamage(20);
        var repairStarted = sut.TryStartRepair();

        upgradeStarted.Should().BeTrue();
        repairStarted.Should().BeTrue();
        sut.OperationLog.Should().Contain("upgrade:start");
        sut.OperationLog.Should().Contain("repair:start");
    }

    private static BuildingUpgradeRepairRuntime CreateSut(
        int buildCost = 1000,
        int maxHp = 100,
        int currentHp = 40,
        int gold = 2000,
        BuildingOperationState state = BuildingOperationState.Idle,
        int repairTicksRequired = 2)
    {
        return new BuildingUpgradeRepairRuntime(
            level: 1,
            maxLevel: 5,
            maxHp: maxHp,
            currentHp: currentHp,
            gold: gold,
            buildCost: buildCost,
            state: state,
            upgradeTicksRequired: 2,
            repairTicksRequired: repairTicksRequired);
    }
}
