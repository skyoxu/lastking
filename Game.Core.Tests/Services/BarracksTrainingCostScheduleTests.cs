using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class BarracksTrainingCostScheduleTests
{
    // ACC:T16.12
    [Fact]
    public void ShouldNotCompleteBeforeDuration_WhenTrainingClockHasNotElapsed()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 2);
        var resources = NewResources(gold: 200, iron: 100);
        runtime.TryEnqueue(
            new BarracksTrainingDefinition(
                "spearman",
                DurationTicks: 3,
                CostStages:
                [
                    new BarracksCostStage(0, 30, 10),
                ]),
            resources).Accepted.Should().BeTrue();

        var beforeDone = runtime.Advance(2, resources);
        var done = runtime.Advance(1, resources);

        beforeDone.CompletedUnits.Should().BeEmpty();
        done.CompletedUnits.Should().Equal("spearman");
    }

    // ACC:T16.10
    [Fact]
    public void ShouldKeepIntegerSafeAndNonNegativeResources_WhenLongSequenceUsesLargeValues()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 64);
        var resources = NewResources(gold: 2_000_000_000, iron: 1_500_000_000);

        for (var index = 0; index < 40; index++)
        {
            var enqueue = runtime.TryEnqueue(
                new BarracksTrainingDefinition(
                    $"unit-{index}",
                    DurationTicks: (index % 3) + 2,
                    CostStages:
                    [
                        new BarracksCostStage(0, 8_000_000, 4_000_000),
                        new BarracksCostStage(2, 3_000_000, 2_000_000),
                    ]),
                resources);
            enqueue.Accepted.Should().BeTrue();

            if (index % 2 == 0)
            {
                _ = runtime.Advance(1, resources);
            }

            if (runtime.Count > 2 && index % 5 == 0)
            {
                var cancel = runtime.TryCancelAt(1, resources);
                cancel.Accepted.Should().BeTrue();
            }
        }

        _ = runtime.Advance(200, resources);

        resources.Gold.Should().BeGreaterOrEqualTo(0);
        resources.Iron.Should().BeGreaterOrEqualTo(0);
        runtime.Diagnostics.Should().OnlyContain(item => item.Gold >= 0 && item.Iron >= 0);
    }

    // ACC:T16.12
    // ACC:T16.13
    // ACC:T16.14
    // ACC:T16.15
    [Fact]
    public void ShouldFollowConfiguredCostSchedule_WhenUsingStagedOrMixedDeductions()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 2);
        var resources = NewResources(gold: 500, iron: 300);
        runtime.TryEnqueue(
            new BarracksTrainingDefinition(
                "knight",
                DurationTicks: 5,
                CostStages:
                [
                    new BarracksCostStage(0, 20, 10),
                    new BarracksCostStage(2, 30, 15),
                    new BarracksCostStage(4, 40, 20),
                ]),
            resources).Accepted.Should().BeTrue();

        resources.Gold.Should().Be(480);
        resources.Iron.Should().Be(290);

        _ = runtime.Advance(1, resources);
        resources.Gold.Should().Be(480);
        resources.Iron.Should().Be(290);

        _ = runtime.Advance(1, resources);
        resources.Gold.Should().Be(450);
        resources.Iron.Should().Be(275);

        _ = runtime.Advance(1, resources);
        resources.Gold.Should().Be(450);
        resources.Iron.Should().Be(275);

        _ = runtime.Advance(1, resources);
        resources.Gold.Should().Be(410);
        resources.Iron.Should().Be(255);

        var cancel = runtime.TryCancelAt(0, resources);
        cancel.Accepted.Should().BeTrue();
        cancel.RefundedGold.Should().Be(90);
        cancel.RefundedIron.Should().Be(45);
        resources.Gold.Should().Be(500);
        resources.Iron.Should().Be(300);
    }

    // ACC:T16.21
    [Fact]
    public void ShouldEnforceBoundaryTimingRules_WhenCapacityEdgesAreCovered()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 2);
        var resources = NewResources(gold: 300, iron: 120);

        var atCapacityMinusOne = runtime.TryEnqueue(
            new BarracksTrainingDefinition("u1", 3, [new BarracksCostStage(1, 10, 0)]),
            resources);
        var atCapacity = runtime.TryEnqueue(
            new BarracksTrainingDefinition("u2", 3, [new BarracksCostStage(2, 10, 0)]),
            resources);
        var overCapacity = runtime.TryEnqueue(
            new BarracksTrainingDefinition("u3", 3, [new BarracksCostStage(1, 10, 0)]),
            resources);

        atCapacityMinusOne.Accepted.Should().BeTrue();
        atCapacity.Accepted.Should().BeTrue();
        overCapacity.Accepted.Should().BeFalse();

        var goldBeforeTick = resources.Gold;
        _ = runtime.Advance(1, resources);
        resources.Gold.Should().Be(goldBeforeTick - 10);
    }

    private static ResourceManager NewResources(int gold, int iron)
    {
        var resources = new ResourceManager(eventBus: null, runId: "task-16", dayNumber: 1);
        var imported = resources.TryImportSnapshot($"{{\"gold\":{gold},\"iron\":{iron},\"populationCap\":50}}");
        imported.Accepted.Should().BeTrue();
        return resources;
    }
}
