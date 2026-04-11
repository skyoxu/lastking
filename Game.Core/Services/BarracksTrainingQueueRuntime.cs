using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed record BarracksCostStage(int TriggerTick, int GoldCost, int IronCost);

public sealed record BarracksTrainingDefinition(
    string UnitType,
    int DurationTicks,
    IReadOnlyList<BarracksCostStage> CostStages);

public sealed record BarracksTrainingJobView(
    int JobId,
    string UnitType,
    int DurationTicks,
    int ElapsedTicks,
    int DeductedGold,
    int DeductedIron);

public sealed record BarracksQueueDiagnosticEntry(
    int OperationIndex,
    string Operation,
    string Transition,
    IReadOnlyList<string> QueueSnapshot,
    int Gold,
    int Iron);

public sealed record BarracksEnqueueResult(
    bool Accepted,
    string Reason,
    int JobId,
    int QueueLength,
    int GoldAfter,
    int IronAfter,
    int DeductedGold,
    int DeductedIron);

public sealed record BarracksCancelResult(
    bool Accepted,
    string Reason,
    int QueueLength,
    int GoldAfter,
    int IronAfter,
    int RefundedGold,
    int RefundedIron);

public sealed record BarracksAdvanceResult(
    int TicksApplied,
    IReadOnlyList<string> CompletedUnits,
    int QueueLength,
    int GoldAfter,
    int IronAfter);

public sealed class BarracksTrainingQueueRuntime
{
    private sealed class JobState
    {
        public required int JobId { get; init; }
        public required string UnitType { get; init; }
        public required int DurationTicks { get; init; }
        public required List<BarracksCostStage> Stages { get; init; }
        public int ElapsedTicks { get; set; }
        public int DeductedGold { get; set; }
        public int DeductedIron { get; set; }
        public int NextStageIndex { get; set; }
    }

    private readonly List<JobState> queue = [];
    private readonly List<BarracksQueueDiagnosticEntry> diagnostics = [];
    private int nextJobId = 1;
    private int operationIndex;

    public BarracksTrainingQueueRuntime(int capacity = 8)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Queue capacity must be greater than zero.");
        }

        Capacity = capacity;
    }

    public int Capacity { get; }

    public int Count => queue.Count;

    public IReadOnlyList<BarracksQueueDiagnosticEntry> Diagnostics => diagnostics;

    public IReadOnlyList<BarracksTrainingJobView> Jobs =>
        queue.Select(static item => new BarracksTrainingJobView(
            item.JobId,
            item.UnitType,
            item.DurationTicks,
            item.ElapsedTicks,
            item.DeductedGold,
            item.DeductedIron)).ToArray();

    public BarracksEnqueueResult TryEnqueue(BarracksTrainingDefinition definition, ResourceManager resources)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(resources);

        if (queue.Count >= Capacity)
        {
            return RejectEnqueue("capacity_reached", resources);
        }

        if (string.IsNullOrWhiteSpace(definition.UnitType))
        {
            return RejectEnqueue("unit_type_required", resources);
        }

        if (definition.DurationTicks <= 0)
        {
            return RejectEnqueue("duration_must_be_positive", resources);
        }

        if (definition.CostStages is null || definition.CostStages.Count == 0)
        {
            return RejectEnqueue("cost_stages_required", resources);
        }

        var orderedStages = definition.CostStages
            .OrderBy(stage => stage.TriggerTick)
            .Select(stage => ValidateStage(stage))
            .ToList();

        if (orderedStages[^1].TriggerTick > definition.DurationTicks)
        {
            return RejectEnqueue("stage_trigger_exceeds_duration", resources);
        }

        if (orderedStages[0].TriggerTick < 0)
        {
            return RejectEnqueue("stage_trigger_negative", resources);
        }

        var initialStages = orderedStages.Where(stage => stage.TriggerTick == 0).ToArray();
        var upfrontGold = checked(initialStages.Sum(static stage => stage.GoldCost));
        var upfrontIron = checked(initialStages.Sum(static stage => stage.IronCost));
        var spend = resources.TrySpend(upfrontGold, upfrontIron, reason: "barracks.enqueue");
        if (!spend.Succeeded)
        {
            return RejectEnqueue("insufficient_resources", resources);
        }

        var job = new JobState
        {
            JobId = nextJobId++,
            UnitType = definition.UnitType.Trim(),
            DurationTicks = definition.DurationTicks,
            Stages = orderedStages,
            ElapsedTicks = 0,
            DeductedGold = upfrontGold,
            DeductedIron = upfrontIron,
            NextStageIndex = initialStages.Length,
        };
        queue.Add(job);
        RecordDiagnostic("enqueue", "accepted", resources);
        return new BarracksEnqueueResult(
            Accepted: true,
            Reason: string.Empty,
            JobId: job.JobId,
            QueueLength: queue.Count,
            GoldAfter: resources.Gold,
            IronAfter: resources.Iron,
            DeductedGold: upfrontGold,
            DeductedIron: upfrontIron);
    }

    public BarracksCancelResult TryCancelAt(int index, ResourceManager resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (index < 0 || index >= queue.Count)
        {
            RecordDiagnostic("cancel", "rejected_invalid_index", resources);
            return new BarracksCancelResult(
                Accepted: false,
                Reason: "invalid_index",
                QueueLength: queue.Count,
                GoldAfter: resources.Gold,
                IronAfter: resources.Iron,
                RefundedGold: 0,
                RefundedIron: 0);
        }

        var job = queue[index];
        queue.RemoveAt(index);
        var refund = resources.TryAdd(job.DeductedGold, job.DeductedIron, 0, reason: "barracks.cancel_refund");
        if (!refund.Succeeded)
        {
            throw new InvalidOperationException("Refund should never fail for non-negative values.");
        }

        RecordDiagnostic("cancel", "accepted", resources);
        return new BarracksCancelResult(
            Accepted: true,
            Reason: string.Empty,
            QueueLength: queue.Count,
            GoldAfter: resources.Gold,
            IronAfter: resources.Iron,
            RefundedGold: job.DeductedGold,
            RefundedIron: job.DeductedIron);
    }

    public BarracksAdvanceResult Advance(int ticks, ResourceManager resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (ticks <= 0)
        {
            RecordDiagnostic("advance", "noop_non_positive_ticks", resources);
            return new BarracksAdvanceResult(
                TicksApplied: 0,
                CompletedUnits: Array.Empty<string>(),
                QueueLength: queue.Count,
                GoldAfter: resources.Gold,
                IronAfter: resources.Iron);
        }

        var completed = new List<string>();
        var applied = 0;
        for (var i = 0; i < ticks; i++)
        {
            if (queue.Count == 0)
            {
                break;
            }

            var head = queue[0];
            head.ElapsedTicks += 1;
            ApplyDueStages(head, resources);
            if (head.ElapsedTicks < head.DurationTicks)
            {
                applied += 1;
                continue;
            }

            queue.RemoveAt(0);
            completed.Add(head.UnitType);
            applied += 1;
        }

        var transition = completed.Count > 0 ? "completed" : "tick";
        RecordDiagnostic("advance", transition, resources);
        return new BarracksAdvanceResult(
            TicksApplied: applied,
            CompletedUnits: completed,
            QueueLength: queue.Count,
            GoldAfter: resources.Gold,
            IronAfter: resources.Iron);
    }

    private BarracksEnqueueResult RejectEnqueue(string reason, ResourceManager resources)
    {
        RecordDiagnostic("enqueue", $"rejected_{reason}", resources);
        return new BarracksEnqueueResult(
            Accepted: false,
            Reason: reason,
            JobId: 0,
            QueueLength: queue.Count,
            GoldAfter: resources.Gold,
            IronAfter: resources.Iron,
            DeductedGold: 0,
            DeductedIron: 0);
    }

    private static BarracksCostStage ValidateStage(BarracksCostStage stage)
    {
        if (stage.GoldCost < 0 || stage.IronCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stage), "Stage costs cannot be negative.");
        }

        return stage;
    }

    private static void ApplyDueStages(JobState job, ResourceManager resources)
    {
        while (job.NextStageIndex < job.Stages.Count)
        {
            var stage = job.Stages[job.NextStageIndex];
            if (stage.TriggerTick > job.ElapsedTicks)
            {
                break;
            }

            var spend = resources.TrySpend(stage.GoldCost, stage.IronCost, reason: "barracks.stage_deduction");
            if (!spend.Succeeded)
            {
                throw new InvalidOperationException("Staged deduction failed due to insufficient resources.");
            }

            job.DeductedGold = checked(job.DeductedGold + stage.GoldCost);
            job.DeductedIron = checked(job.DeductedIron + stage.IronCost);
            job.NextStageIndex += 1;
        }
    }

    private void RecordDiagnostic(string operation, string transition, ResourceManager resources)
    {
        operationIndex += 1;
        diagnostics.Add(new BarracksQueueDiagnosticEntry(
            OperationIndex: operationIndex,
            Operation: operation,
            Transition: transition,
            QueueSnapshot: queue.Select(job => $"{job.JobId}:{job.UnitType}@{job.ElapsedTicks}/{job.DurationTicks}").ToArray(),
            Gold: resources.Gold,
            Iron: resources.Iron));
    }
}
