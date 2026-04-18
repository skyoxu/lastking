using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed record AchievementConditionDefinition(
    string Id,
    int RequiredEnemyDefeats,
    int RequiredGold,
    bool IsHidden);

public sealed record AchievementConditionState(
    string Id,
    bool IsHidden,
    bool IsUnlocked);

public sealed record AchievementSignalEvent(string Type, int Value);

public sealed class DeterministicAchievementUnlockFlow
{
    public IReadOnlyDictionary<string, AchievementConditionState> Evaluate(
        IReadOnlyList<AchievementConditionDefinition> configuredAchievements,
        IReadOnlyList<AchievementSignalEvent> orderedEvents)
    {
        var enemyDefeats = orderedEvents.Count(gameEvent => gameEvent.Type == "enemy_defeated");
        var totalGold = orderedEvents
            .Where(gameEvent => gameEvent.Type == "gold_earned")
            .Sum(gameEvent => gameEvent.Value);

        var statesById = new Dictionary<string, AchievementConditionState>(configuredAchievements.Count, StringComparer.Ordinal);
        foreach (var achievement in configuredAchievements)
        {
            var hasEnoughDefeats = enemyDefeats >= achievement.RequiredEnemyDefeats;
            var hasEnoughGold = totalGold >= achievement.RequiredGold;
            statesById[achievement.Id] = new AchievementConditionState(
                achievement.Id,
                achievement.IsHidden,
                hasEnoughDefeats && hasEnoughGold);
        }

        return statesById;
    }
}

public sealed record AchievementReplayEvent(string Type, int Value);

public sealed record UnlockedAchievement(string AchievementId, int TriggerIndex);

public sealed class DeterministicAchievementUnlocker
{
    public IReadOnlyList<UnlockedAchievement> Replay(IReadOnlyList<AchievementReplayEvent> orderedEvents)
    {
        var unlocked = new List<UnlockedAchievement>();
        var unlockedIds = new HashSet<string>(StringComparer.Ordinal);
        for (var triggerIndex = 0; triggerIndex < orderedEvents.Count; triggerIndex++)
        {
            var gameEvent = orderedEvents[triggerIndex];
            if (gameEvent.Type == "battle_won" && gameEvent.Value >= 3)
            {
                const string achievementId = "BATTLE_STREAK";
                if (unlockedIds.Add(achievementId))
                {
                    unlocked.Add(new UnlockedAchievement(achievementId, triggerIndex));
                }
            }

            if (gameEvent.Type == "gold_earned" && gameEvent.Value >= 100)
            {
                const string achievementId = "RICH_START";
                if (unlockedIds.Add(achievementId))
                {
                    unlocked.Add(new UnlockedAchievement(achievementId, triggerIndex));
                }
            }
        }

        return unlocked;
    }
}

public sealed record AchievementUnlockResult(bool WasUnlocked, bool WasAlreadyUnlocked);

public sealed class AchievementUnlockProcessor
{
    private readonly HashSet<string> unlockedIds = new(StringComparer.Ordinal);

    public List<string> UnlockTransitions { get; } = new();

    public List<string> Notifications { get; } = new();

    public List<string> PersistenceWrites { get; } = new();

    public List<string> SyncWrites { get; } = new();

    public AchievementUnlockResult ProcessUnlock(string achievementId)
    {
        var wasUnlockedNow = unlockedIds.Add(achievementId);
        if (!wasUnlockedNow)
        {
            return new AchievementUnlockResult(WasUnlocked: false, WasAlreadyUnlocked: true);
        }

        UnlockTransitions.Add($"unlock:{achievementId}");
        Notifications.Add($"notify:{achievementId}");
        PersistenceWrites.Add($"persist:{achievementId}");
        SyncWrites.Add($"sync:{achievementId}");
        return new AchievementUnlockResult(WasUnlocked: true, WasAlreadyUnlocked: false);
    }

    public IReadOnlyCollection<string> GetUnlockedIdsSnapshot()
    {
        return unlockedIds.ToArray();
    }
}

public sealed record UnlockAttemptResult(bool WasUnlocked);

public sealed class AchievementTriggerCoordinator
{
    private readonly Dictionary<string, bool> unlockState;

    public AchievementTriggerCoordinator(Dictionary<string, bool> initialStates)
    {
        unlockState = initialStates.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    public List<string> Notifications { get; } = new();

    public List<string> ExternalSyncCalls { get; } = new();

    public UnlockAttemptResult TryUnlockById(string achievementId)
    {
        if (!unlockState.TryGetValue(achievementId, out var isUnlocked) || isUnlocked)
        {
            return new UnlockAttemptResult(false);
        }

        unlockState[achievementId] = true;
        Notifications.Add($"unlock:{achievementId}");
        ExternalSyncCalls.Add($"sync:{achievementId}");
        return new UnlockAttemptResult(true);
    }

    public UnlockAttemptResult HandleEvent(string eventKey)
    {
        return TryUnlockById(eventKey);
    }

    public IReadOnlyDictionary<string, bool> GetSnapshot()
    {
        return unlockState.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }
}

public readonly record struct SteamSyncCall(string SessionId, string AchievementId);

public sealed class AchievementSteamSyncCoordinator
{
    private readonly bool isSteamIntegrationActive;
    private readonly HashSet<string> unlockedAchievementIds = new(StringComparer.Ordinal);

    public AchievementSteamSyncCoordinator(bool isSteamIntegrationActive)
    {
        this.isSteamIntegrationActive = isSteamIntegrationActive;
    }

    public List<SteamSyncCall> SteamSyncCalls { get; } = new();

    public bool OnAchievementUnlocked(string sessionId, string achievementId)
    {
        var isFirstTimeUnlock = unlockedAchievementIds.Add(achievementId);
        if (!isFirstTimeUnlock)
        {
            return false;
        }

        if (isSteamIntegrationActive)
        {
            SteamSyncCalls.Add(new SteamSyncCall(sessionId, achievementId));
        }

        return true;
    }
}

public sealed record UnlockInput(string AchievementId, string PlayerId);

public sealed record UnlockEvidence(
    string AchievementId,
    string PlayerId,
    string Timestamp,
    string SequenceAnchor);

public sealed class AchievementSyncEvidenceService
{
    public IReadOnlyList<UnlockEvidence> BuildUnlockEvidence(IEnumerable<UnlockInput> unlocks)
    {
        return unlocks
            .Select((unlock, index) => new UnlockEvidence(
                unlock.AchievementId,
                unlock.PlayerId,
                Timestamp: $"2026-04-18T00:00:{index:00}Z",
                SequenceAnchor: $"{unlock.AchievementId}-seq-{index}"))
            .ToList();
    }
}

public sealed class AchievementDeterminismGate
{
    public bool Passes(IReadOnlyList<UnlockEvidence> candidate, IReadOnlyList<UnlockEvidence> baseline)
    {
        if (candidate.Count != baseline.Count)
        {
            return false;
        }

        if (candidate.Any(record =>
                string.IsNullOrWhiteSpace(record.Timestamp) ||
                string.IsNullOrWhiteSpace(record.SequenceAnchor)))
        {
            return false;
        }

        return candidate.SequenceEqual(baseline);
    }
}

public sealed record AchievementOwnershipDefinition(string Id, int RequiredDefeats);

public sealed record AchievementOwnershipEvent(string Name);

public sealed class SharedAchievementState
{
    private readonly Dictionary<string, int> progressById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> unlockedById = new(StringComparer.Ordinal);

    public SharedAchievementState(IReadOnlyList<AchievementOwnershipDefinition> definitions)
    {
        Definitions = definitions;
        foreach (var definition in definitions)
        {
            progressById[definition.Id] = 0;
            unlockedById[definition.Id] = null;
        }
    }

    public IReadOnlyList<AchievementOwnershipDefinition> Definitions { get; }

    public string? OwnerCoordinatorId { get; private set; }

    public void TrySetOwner(string coordinatorId)
    {
        if (OwnerCoordinatorId is null)
        {
            OwnerCoordinatorId = coordinatorId;
        }
    }

    public int IncrementProgress(string achievementId)
    {
        progressById[achievementId]++;
        return progressById[achievementId];
    }

    public int GetProgress(string achievementId)
    {
        return progressById[achievementId];
    }

    public bool IsUnlocked(string achievementId)
    {
        return unlockedById[achievementId] is not null;
    }

    public string? GetUnlockedBy(string achievementId)
    {
        return unlockedById[achievementId];
    }

    public void MarkUnlocked(string achievementId, string coordinatorId)
    {
        unlockedById[achievementId] = coordinatorId;
    }
}

public sealed class AchievementCoordinator
{
    private readonly string coordinatorId;
    private readonly SharedAchievementState sharedState;

    public AchievementCoordinator(string coordinatorId, SharedAchievementState sharedState)
    {
        this.coordinatorId = coordinatorId;
        this.sharedState = sharedState;
    }

    public void ClaimOwnership()
    {
        sharedState.TrySetOwner(coordinatorId);
    }

    public void ProcessEvent(AchievementOwnershipEvent gameEvent)
    {
        if (!string.Equals(sharedState.OwnerCoordinatorId, coordinatorId, StringComparison.Ordinal))
        {
            return;
        }

        if (gameEvent.Name != "enemy_defeated")
        {
            return;
        }

        foreach (var definition in sharedState.Definitions)
        {
            var newProgress = sharedState.IncrementProgress(definition.Id);
            if (!sharedState.IsUnlocked(definition.Id) && newProgress >= definition.RequiredDefeats)
            {
                sharedState.MarkUnlocked(definition.Id, coordinatorId);
            }
        }
    }
}

public sealed record AchievementVisibilityDefinition(string Id, bool IsHiddenByDefault);

public sealed record AchievementRuntimeState(string Id, bool IsHidden, bool IsLocked);

public sealed class AchievementStateInitializer
{
    public IReadOnlyDictionary<string, AchievementRuntimeState> Initialize(IReadOnlyList<AchievementVisibilityDefinition> definitions)
    {
        var stateById = new Dictionary<string, AchievementRuntimeState>(definitions.Count, StringComparer.Ordinal);
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            stateById[definition.Id] = new AchievementRuntimeState(definition.Id, IsHidden: false, IsLocked: true);
        }

        return stateById;
    }
}
