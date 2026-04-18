using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core.Repositories;
using Game.Core.Services;
using Godot;
using Godot.Collections;

namespace Game.Godot.Adapters.Achievements;

public partial class AchievementRuntimeTestBridge : Node
{
    private readonly DeterministicAchievementUnlockFlow unlockFlow = new();
    private readonly DeterministicAchievementUnlocker replayUnlocker = new();
    private AchievementUnlockProcessor unlockProcessor = new();

    public Dictionary BuildSessionStartRows(Array definitions)
    {
        var coreDefinitions = ParseConditionDefinitions(definitions);
        var states = unlockFlow.Evaluate(coreDefinitions, new List<AchievementSignalEvent>());
        var rows = new Array<Dictionary>();
        foreach (var definition in coreDefinitions)
        {
            var state = states[definition.Id];
            rows.Add(new Dictionary
            {
                { "id", definition.Id },
                { "hidden", state.IsHidden },
                { "unlocked", state.IsUnlocked }
            });
        }

        return new Dictionary
        {
            { "ok", true },
            { "rows", rows }
        };
    }

    public Dictionary EvaluateVisibilityAndUnlock(Array definitions, Array events)
    {
        var coreDefinitions = ParseConditionDefinitions(definitions);
        var coreEvents = ParseSignalEvents(events);
        var states = unlockFlow.Evaluate(coreDefinitions, coreEvents);

        var rows = new Array<Dictionary>();
        foreach (var definition in coreDefinitions)
        {
            var state = states[definition.Id];
            rows.Add(new Dictionary
            {
                { "id", definition.Id },
                { "hidden", state.IsHidden },
                { "unlocked", state.IsUnlocked }
            });
        }

        return new Dictionary
        {
            { "ok", true },
            { "rows", rows }
        };
    }

    public Dictionary SimulateUnlockNotifications(Array replayEvents)
    {
        var events = ParseReplayEvents(replayEvents);
        var unlocked = replayUnlocker.Replay(events);
        unlockProcessor = new AchievementUnlockProcessor();

        foreach (var unlock in unlocked)
        {
            _ = unlockProcessor.ProcessUnlock(unlock.AchievementId);
        }

        return new Dictionary
        {
            { "ok", true },
            { "unlock_ids", ToGodotStringArray(unlocked.Select(record => record.AchievementId)) },
            { "unlock_trigger_indices", ToGodotIntArray(unlocked.Select(record => record.TriggerIndex)) },
            { "notifications", ToGodotStringArray(unlockProcessor.Notifications) },
            { "persistence_writes", ToGodotStringArray(unlockProcessor.PersistenceWrites) },
            { "sync_writes", ToGodotStringArray(unlockProcessor.SyncWrites) }
        };
    }

    public Dictionary SimulateSteamSync(bool steamActive, string sessionId, Array unlockIds)
    {
        var coordinator = new AchievementSteamSyncCoordinator(steamActive);
        foreach (var unlockIdVariant in unlockIds)
        {
            var unlockId = unlockIdVariant.ToString();
            if (!string.IsNullOrWhiteSpace(unlockId))
            {
                _ = coordinator.OnAchievementUnlocked(sessionId, unlockId);
            }
        }

        return new Dictionary
        {
            { "ok", true },
            { "steam_sync_count", coordinator.SteamSyncCalls.Count },
            { "steam_sync_ids", ToGodotStringArray(coordinator.SteamSyncCalls.Select(call => call.AchievementId)) }
        };
    }

    public Dictionary SimulateLoadDefinitions(string configPath)
    {
        var loader = new AchievementDefinitionLoader();
        try
        {
            var definitions = loader.LoadAtStartup(configPath);
            return new Dictionary
            {
                { "ok", true },
                { "count", definitions.Count },
                { "ids", ToGodotStringArray(definitions.Select(record => record.Id)) }
            };
        }
        catch (System.Exception ex)
        {
            return new Dictionary
            {
                { "ok", false },
                { "error", ex.Message }
            };
        }
    }

    public Dictionary SimulateLoadDefinitionsFromJson(string relativePath, string jsonText)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(relativePath)
            ? Path.Combine("logs", "tmp", "task27-achievements.json")
            : relativePath;
        var filePath = normalizedPath.Replace('/', Path.DirectorySeparatorChar);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, jsonText);
        return SimulateLoadDefinitions(normalizedPath);
    }

    private static IReadOnlyList<AchievementConditionDefinition> ParseConditionDefinitions(Array definitions)
    {
        var parsed = new List<AchievementConditionDefinition>(definitions.Count);
        foreach (var variant in definitions)
        {
            if (variant.VariantType != Variant.Type.Dictionary)
            {
                continue;
            }
            var item = variant.AsGodotDictionary();

            var id = item.TryGetValue("id", out var idValue) ? idValue.ToString() : string.Empty;
            var requiredEnemyDefeats = item.TryGetValue("required_enemy_defeats", out var defeatsValue) ? ToInt(defeatsValue) : 0;
            var requiredGold = item.TryGetValue("required_gold", out var goldValue) ? ToInt(goldValue) : 0;
            var isHidden = item.TryGetValue("is_hidden", out var hiddenValue) && ToBool(hiddenValue);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            parsed.Add(new AchievementConditionDefinition(
                id,
                requiredEnemyDefeats,
                requiredGold,
                isHidden));
        }

        return parsed;
    }

    private static IReadOnlyList<AchievementSignalEvent> ParseSignalEvents(Array events)
    {
        var parsed = new List<AchievementSignalEvent>(events.Count);
        foreach (var variant in events)
        {
            if (variant.VariantType != Variant.Type.Dictionary)
            {
                continue;
            }
            var item = variant.AsGodotDictionary();

            var type = item.TryGetValue("type", out var typeValue) ? typeValue.ToString() : string.Empty;
            if (string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            var value = item.TryGetValue("value", out var valueVariant) ? ToInt(valueVariant) : 0;
            parsed.Add(new AchievementSignalEvent(type, value));
        }

        return parsed;
    }

    private static IReadOnlyList<AchievementReplayEvent> ParseReplayEvents(Array events)
    {
        var parsed = new List<AchievementReplayEvent>(events.Count);
        foreach (var variant in events)
        {
            if (variant.VariantType != Variant.Type.Dictionary)
            {
                continue;
            }
            var item = variant.AsGodotDictionary();

            var type = item.TryGetValue("type", out var typeValue) ? typeValue.ToString() : string.Empty;
            if (string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            var value = item.TryGetValue("value", out var valueVariant) ? ToInt(valueVariant) : 0;
            parsed.Add(new AchievementReplayEvent(type, value));
        }

        return parsed;
    }

    private static int ToInt(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Int => (int)value,
            Variant.Type.Float => (int)(double)value,
            Variant.Type.String when int.TryParse(value.AsString(), out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ToBool(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Bool => (bool)value,
            Variant.Type.Int => (int)value != 0,
            Variant.Type.String when bool.TryParse(value.AsString(), out var parsed) => parsed,
            _ => false
        };
    }

    private static Array<string> ToGodotStringArray(IEnumerable<string> values)
    {
        var result = new Array<string>();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static Array<int> ToGodotIntArray(IEnumerable<int> values)
    {
        var result = new Array<int>();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }
}
