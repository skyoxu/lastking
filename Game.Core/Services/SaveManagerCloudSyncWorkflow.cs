using System;
using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;

namespace Game.Core.Services;

public sealed record SaveWorkflowCommand(
    string SlotId,
    bool EnableCloudSync,
    string SteamAccountId,
    string Payload);

public sealed class SaveManagerCloudSyncWorkflow
{
    private readonly ICloudSaveSyncService _cloudSaveSyncService;

    public SaveManagerCloudSyncWorkflow(ICloudSaveSyncService cloudSaveSyncService)
    {
        _cloudSaveSyncService = cloudSaveSyncService ?? throw new ArgumentNullException(nameof(cloudSaveSyncService));
    }

    public int[] RunDeterministicLoop(int[] enemyPositions, int[] targetThreatPriority, uint seed, int ticks)
    {
        if (enemyPositions is null) throw new ArgumentNullException(nameof(enemyPositions));
        if (targetThreatPriority is null) throw new ArgumentNullException(nameof(targetThreatPriority));
        if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));

        var random = new Random(unchecked((int)seed));
        var outputs = new int[ticks];
        var positionSum = 0;
        var threatSum = 0;

        foreach (var position in enemyPositions)
        {
            positionSum += position;
        }
        foreach (var threat in targetThreatPriority)
        {
            threatSum += threat;
        }

        for (var index = 0; index < ticks; index++)
        {
            outputs[index] = positionSum + threatSum - random.Next(0, 4);
        }

        return outputs;
    }

    public CloudSaveSyncResultDto? Save(SaveWorkflowCommand command)
    {
        if (!command.EnableCloudSync
            || string.IsNullOrWhiteSpace(command.SteamAccountId)
            || string.IsNullOrWhiteSpace(command.SlotId)
            || string.IsNullOrWhiteSpace(command.Payload))
        {
            return null;
        }

        var runId = $"task26-save-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return _cloudSaveSyncService.Sync(
            runId: runId,
            slotId: command.SlotId,
            direction: "upload",
            steamAccountId: command.SteamAccountId,
            payload: command.Payload);
    }
}

