using FluentAssertions;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public class BuildingUpgradeRepairRefundTests
{
    // ACC:T15.23
    [Fact]
    public void ShouldRefundOnlyUnconsumedRemainingCost_WhenCancellationInterruptsActiveUpgrade()
    {
        var sut = CreateSut(upgradeTicksRequired: 4, initialGold: 1_000, level: 1, currentHp: 60);
        sut.OverrideUpgradeCost(level: 1, cost: 101);

        var started = sut.TryStartUpgrade();
        started.Should().BeTrue();

        sut.AdvanceTick();
        sut.AdvanceTick();
        sut.AdvanceTick();

        var deductedBeforeCancel = sut.TotalDeducted;
        var consumedBeforeCancel = sut.TotalConsumed;
        var expectedRefund = deductedBeforeCancel - consumedBeforeCancel;

        var refund = sut.CancelActiveOperation();

        refund.Should().Be(expectedRefund);
        refund.Should().BeGreaterOrEqualTo(0);
        refund.Should().BeLessOrEqualTo(deductedBeforeCancel);
    }

    [Fact]
    public void ShouldNeverExceedDeductedValue_WhenInterruptionOccursDuringActiveRepair()
    {
        var sut = CreateSut(repairTicksRequired: 3, initialGold: 1_000, level: 1, currentHp: 30, buildCost: 198);

        var started = sut.TryStartRepair();
        started.Should().BeTrue();

        sut.AdvanceTick();

        var deductedBeforeInterrupt = sut.TotalDeducted;
        var consumedBeforeInterrupt = sut.TotalConsumed;
        var expectedRefund = deductedBeforeInterrupt - consumedBeforeInterrupt;

        var refund = sut.InterruptActiveOperation();

        refund.Should().Be(expectedRefund);
        refund.Should().BeLessOrEqualTo(deductedBeforeInterrupt);
    }

    [Fact]
    public void ShouldKeepStateUnchanged_WhenCancelRequestedWithoutActiveOperation()
    {
        var sut = CreateSut(initialGold: 500);
        var goldBeforeCancel = sut.Gold;
        var deductedBeforeCancel = sut.TotalDeducted;
        var consumedBeforeCancel = sut.TotalConsumed;

        var refund = sut.CancelActiveOperation();

        refund.Should().Be(0);
        sut.Gold.Should().Be(goldBeforeCancel);
        sut.TotalDeducted.Should().Be(deductedBeforeCancel);
        sut.TotalConsumed.Should().Be(consumedBeforeCancel);
        sut.State.Should().Be(BuildingOperationState.Idle);
    }

    private static BuildingUpgradeRepairRuntime CreateSut(
        int upgradeTicksRequired = 2,
        int repairTicksRequired = 2,
        int initialGold = 1_000,
        int level = 1,
        int currentHp = 40,
        int buildCost = 200)
    {
        return new BuildingUpgradeRepairRuntime(
            level: level,
            maxLevel: 5,
            maxHp: 100,
            currentHp: currentHp,
            gold: initialGold,
            buildCost: buildCost,
            state: BuildingOperationState.Idle,
            upgradeTicksRequired: upgradeTicksRequired,
            repairTicksRequired: repairTicksRequired);
    }
}
