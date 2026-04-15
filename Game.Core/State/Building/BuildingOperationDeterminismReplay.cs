using System.Collections.Generic;
using System.Linq;

namespace Game.Core.State.Building;

public sealed record BuildingOperationPlan(string BuildingId, string OperationType, int RequiredTicks);

public sealed record BuildingOperationReplayResult(
    IReadOnlyList<string> TickTimeline,
    IReadOnlyList<string> CompletedOperations,
    IReadOnlyList<string> PendingOperations);

public sealed class BuildingOperationDeterminismReplay
{
    private readonly List<MutableOperation> pending;

    public BuildingOperationDeterminismReplay(IReadOnlyList<BuildingOperationPlan> initialPlan)
    {
        pending = initialPlan
            .Select(item => new MutableOperation(item.BuildingId, item.OperationType, item.RequiredTicks))
            .ToList();
    }

    public BuildingOperationReplayResult Replay(IReadOnlyList<int> progressSteps)
    {
        var timeline = new List<string>();
        var completed = new List<string>();

        for (var tick = 0; tick < progressSteps.Count; tick++)
        {
            var step = progressSteps[tick];
            timeline.Add($"tick:{tick}:step:{step}");
            if (step <= 0)
            {
                continue;
            }

            var ordered = pending
                .OrderBy(item => item.BuildingId)
                .ThenBy(item => item.OperationType)
                .ToList();
            foreach (var item in ordered)
            {
                item.ElapsedTicks += step;
                timeline.Add($"{item.BuildingId}:{item.OperationType}:elapsed:{item.ElapsedTicks}");
                if (item.ElapsedTicks >= item.RequiredTicks)
                {
                    var marker = $"{item.BuildingId}:{item.OperationType}";
                    completed.Add(marker);
                    timeline.Add($"{marker}:completed");
                    pending.Remove(item);
                }
            }
        }

        var pendingMarkers = pending
            .Select(item => $"{item.BuildingId}:{item.OperationType}")
            .ToArray();

        return new BuildingOperationReplayResult(timeline, completed, pendingMarkers);
    }

    private sealed class MutableOperation
    {
        public MutableOperation(string buildingId, string operationType, int requiredTicks)
        {
            BuildingId = buildingId;
            OperationType = operationType;
            RequiredTicks = requiredTicks;
        }

        public string BuildingId { get; }

        public string OperationType { get; }

        public int RequiredTicks { get; }

        public int ElapsedTicks { get; set; }
    }
}
