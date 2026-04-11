using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class BarracksTrainingQueueTests
{
    // ACC:T16.1
    [Fact]
    public void ShouldDefineSingleQueueContract_WhenBarracksTrainingQueueIsUsed()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 4);
        var resources = NewResources(gold: 300, iron: 120);

        var first = runtime.TryEnqueue(Upfront("spearman", durationTicks: 3, goldCost: 40, ironCost: 10), resources);
        var second = runtime.TryEnqueue(Upfront("archer", durationTicks: 2, goldCost: 30, ironCost: 8), resources);
        var cancelSecond = runtime.TryCancelAt(index: 1, resources);

        first.Accepted.Should().BeTrue();
        second.Accepted.Should().BeTrue();
        cancelSecond.Accepted.Should().BeTrue();
        cancelSecond.RefundedGold.Should().Be(second.DeductedGold);
        cancelSecond.RefundedIron.Should().Be(second.DeductedIron);
        runtime.Jobs.Should().ContainSingle(job => job.UnitType == "spearman");
    }

    // ACC:T16.4
    [Fact]
    public void ShouldKeepIndependentQueueState_WhenMultipleBarracksOperate()
    {
        var firstRuntime = new BarracksTrainingQueueRuntime(capacity: 3);
        var secondRuntime = new BarracksTrainingQueueRuntime(capacity: 3);
        var firstResources = NewResources(gold: 220, iron: 100);
        var secondResources = NewResources(gold: 220, iron: 100);

        firstRuntime.TryEnqueue(Upfront("spearman", 2, 50, 20), firstResources).Accepted.Should().BeTrue();
        secondRuntime.TryEnqueue(Upfront("cavalry", 3, 70, 30), secondResources).Accepted.Should().BeTrue();
        var secondElapsedBefore = secondRuntime.Jobs[0].ElapsedTicks;
        firstRuntime.TryCancelAt(0, firstResources).Accepted.Should().BeTrue();
        _ = firstRuntime.Advance(1, firstResources);

        firstRuntime.Count.Should().Be(0);
        secondRuntime.Count.Should().Be(1);
        secondRuntime.Jobs[0].UnitType.Should().Be("cavalry");
        secondRuntime.Jobs[0].ElapsedTicks.Should().Be(secondElapsedBefore);
        secondResources.Gold.Should().Be(150);
        secondResources.Iron.Should().Be(70);
    }

    // ACC:T16.5
    [Fact]
    public void ShouldApplyRequestTimeDeductionRules_WhenEnqueueAcceptedOrRejected()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 2);
        var resources = NewResources(gold: 100, iron: 40);

        var accepted = runtime.TryEnqueue(Upfront("spearman", 3, 60, 20), resources);
        var rejected = runtime.TryEnqueue(Upfront("cavalry", 3, 90, 30), resources);

        accepted.Accepted.Should().BeTrue();
        accepted.DeductedGold.Should().Be(60);
        accepted.DeductedIron.Should().Be(20);
        resources.Gold.Should().Be(40);
        resources.Iron.Should().Be(20);

        rejected.Accepted.Should().BeFalse();
        rejected.Reason.Should().Be("insufficient_resources");
        runtime.Count.Should().Be(1);
        resources.Gold.Should().Be(40);
        resources.Iron.Should().Be(20);
    }

    // ACC:T16.6
    [Fact]
    public void ShouldEmitExactlyOneCompletionPerHeadJob_WhenQueueAdvancesInFifoOrder()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 3);
        var resources = NewResources(gold: 300, iron: 150);
        runtime.TryEnqueue(Upfront("spearman", durationTicks: 2, goldCost: 30, ironCost: 10), resources).Accepted.Should().BeTrue();
        runtime.TryEnqueue(Upfront("archer", durationTicks: 1, goldCost: 20, ironCost: 6), resources).Accepted.Should().BeTrue();

        var tick1 = runtime.Advance(1, resources);
        var tick2 = runtime.Advance(1, resources);
        var tick3 = runtime.Advance(1, resources);
        var tick4 = runtime.Advance(1, resources);

        tick1.CompletedUnits.Should().BeEmpty();
        tick2.CompletedUnits.Should().Equal("spearman");
        tick3.CompletedUnits.Should().Equal("archer");
        tick4.CompletedUnits.Should().BeEmpty();

        var allCompleted = tick1.CompletedUnits
            .Concat(tick2.CompletedUnits)
            .Concat(tick3.CompletedUnits)
            .Concat(tick4.CompletedUnits)
            .ToArray();
        allCompleted.Should().Equal("spearman", "archer");
        allCompleted.Should().OnlyHaveUniqueItems();
    }

    // ACC:T16.7
    // ACC:T16.8
    [Fact]
    public void ShouldCancelTargetedJobOnly_WhenCancelIndexIsValid()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 4);
        var resources = NewResources(gold: 250, iron: 120);
        runtime.TryEnqueue(Upfront("spearman", 3, 40, 10), resources).Accepted.Should().BeTrue();
        runtime.TryEnqueue(Upfront("archer", 4, 35, 12), resources).Accepted.Should().BeTrue();
        runtime.TryEnqueue(Upfront("cavalry", 5, 60, 20), resources).Accepted.Should().BeTrue();
        var goldBeforeCancel = resources.Gold;
        var ironBeforeCancel = resources.Iron;

        var cancelMiddle = runtime.TryCancelAt(1, resources);
        var cancelInvalid = runtime.TryCancelAt(99, resources);

        cancelMiddle.Accepted.Should().BeTrue();
        runtime.Jobs.Select(job => job.UnitType).Should().Equal("spearman", "cavalry");
        resources.Gold.Should().Be(goldBeforeCancel + cancelMiddle.RefundedGold);
        resources.Iron.Should().Be(ironBeforeCancel + cancelMiddle.RefundedIron);

        var goldBeforeInvalidCancel = resources.Gold;
        var ironBeforeInvalidCancel = resources.Iron;
        cancelInvalid.Accepted.Should().BeFalse();
        cancelInvalid.Reason.Should().Be("invalid_index");
        runtime.Jobs.Select(job => job.UnitType).Should().Equal("spearman", "cavalry");
        resources.Gold.Should().Be(goldBeforeInvalidCancel);
        resources.Iron.Should().Be(ironBeforeInvalidCancel);
    }

    // ACC:T16.11
    [Fact]
    public void ShouldContinueWithNextJob_WhenHeadJobIsCancelled()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 3);
        var resources = NewResources(gold: 240, iron: 120);
        runtime.TryEnqueue(Upfront("spearman", 4, 40, 10), resources).Accepted.Should().BeTrue();
        runtime.TryEnqueue(Upfront("archer", 2, 30, 8), resources).Accepted.Should().BeTrue();

        runtime.Advance(1, resources);
        var cancelHead = runtime.TryCancelAt(0, resources);
        var completion = runtime.Advance(2, resources);

        cancelHead.Accepted.Should().BeTrue();
        completion.CompletedUnits.Should().Equal("archer");
        completion.CompletedUnits.Should().NotContain("spearman");
    }

    // ACC:T16.17
    [Fact]
    public void ShouldExposeQueueDataStructureContract_WhenBarracksIsAudited()
    {
        var queueField = typeof(BarracksTrainingQueueRuntime)
            .GetField("queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        queueField.Should().NotBeNull();
        queueField!.FieldType.IsGenericType.Should().BeTrue();
        queueField.FieldType.GetGenericTypeDefinition().Should().Be(typeof(List<>));
    }

    // ACC:T16.18
    [Fact]
    public void ShouldCoverQueueTransitionsUnderStress_WhenRegressionRuns()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 64);
        var resources = NewResources(gold: 50_000, iron: 25_000);

        for (var i = 0; i < 50; i++)
        {
            runtime.TryEnqueue(Upfront($"unit-{i}", durationTicks: (i % 4) + 1, goldCost: 7, ironCost: 3), resources).Accepted.Should().BeTrue();
            if (i % 5 == 0)
            {
                _ = runtime.Advance(1, resources);
            }

            if (runtime.Count > 3 && i % 7 == 0)
            {
                _ = runtime.TryCancelAt(1, resources);
            }
        }

        var ids = runtime.Diagnostics.Select(item => item.OperationIndex).ToArray();
        ids.Should().BeInAscendingOrder();
        ids.Should().OnlyHaveUniqueItems();
    }

    // ACC:T16.20
    // ACC:T16.22
    // ACC:T16.23
    [Fact]
    public void ShouldRejectWhenQueueAtCapacity_WhenBoundaryAndDiagnosticsAreChecked()
    {
        var runtime = new BarracksTrainingQueueRuntime(capacity: 2);
        var resources = NewResources(gold: 300, iron: 120);

        runtime.TryEnqueue(Upfront("u1", 3, 20, 5), resources).Accepted.Should().BeTrue();
        runtime.TryEnqueue(Upfront("u2", 3, 20, 5), resources).Accepted.Should().BeTrue();
        var rejected = runtime.TryEnqueue(Upfront("u3", 3, 20, 5), resources);

        rejected.Accepted.Should().BeFalse();
        rejected.Reason.Should().Be("capacity_reached");
        runtime.Count.Should().Be(2);

        var lastDiagnostic = runtime.Diagnostics[^1];
        lastDiagnostic.Operation.Should().Be("enqueue");
        lastDiagnostic.Transition.Should().Contain("rejected");
        lastDiagnostic.QueueSnapshot.Should().HaveCount(2);
        lastDiagnostic.OperationIndex.Should().Be(runtime.Diagnostics.Count);
    }

    private static ResourceManager NewResources(int gold, int iron)
    {
        var resources = new ResourceManager(eventBus: null, runId: "task-16", dayNumber: 1);
        var imported = resources.TryImportSnapshot($"{{\"gold\":{gold},\"iron\":{iron},\"populationCap\":50}}");
        imported.Accepted.Should().BeTrue();
        return resources;
    }

    private static BarracksTrainingDefinition Upfront(string unitType, int durationTicks, int goldCost, int ironCost)
    {
        return new BarracksTrainingDefinition(
            unitType,
            durationTicks,
            new[]
            {
                new BarracksCostStage(0, goldCost, ironCost),
            });
    }
}
