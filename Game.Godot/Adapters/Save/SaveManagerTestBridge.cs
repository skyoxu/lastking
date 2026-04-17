using System;
using System.Collections.Generic;
using System.Text.Json;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Services;
using Game.Core.State;
using Godot;

namespace Game.Godot.Adapters.Save;

public partial class SaveManagerTestBridge : Node
{
    private const string StorageKey = "task25-real-save-bridge";
    private DataStoreAdapter? store;
    private TrackingStore? trackingStore;
    private GameStateManager? manager;
    private UIFeedbackPipeline? feedback;
    private readonly List<string> writeKeys = [];
    private string lastLoadReasonCode = "not_run";
    private bool lastLoadOk;

    public override void _Ready()
    {
        EnsureInitialized();
    }

    public void ResetRuntime(
        string storageKey = StorageKey,
        bool enableCompression = false,
        int dayNightSeed = 20250425,
        int dayDurationSeconds = 10,
        int nightDurationSeconds = 10,
        int maxDay = 15)
    {
        writeKeys.Clear();
        lastLoadReasonCode = "not_run";
        lastLoadOk = false;
        feedback = new UIFeedbackPipeline();
        store = new DataStoreAdapter();
        AddChild(store);
        trackingStore = new TrackingStore(store, writeKeys);

        var options = new GameStateManagerOptions(
            StorageKey: storageKey,
            EnableCompression: enableCompression,
            AutoSaveInterval: TimeSpan.FromSeconds(30),
            MaxSaves: 10);
        manager = new GameStateManager(
            store: trackingStore,
            options: options,
            dayNightSeed: dayNightSeed,
            dayNightConfig: new DayNightCycleConfig(dayDurationSeconds, nightDurationSeconds, maxDay));

        manager.SetState(
            new GameState(
                Id: "task25-default-state",
                Level: 1,
                Score: 0,
                Health: 100,
                Inventory: new[] { "wood", "stone" },
                Position: new Position(0, 0),
                Timestamp: DateTime.UtcNow),
            new GameConfig(
                MaxLevel: 99,
                InitialHealth: 100,
                ScoreMultiplier: 1.0,
                AutoSave: true,
                Difficulty: Difficulty.Medium));
    }

    public bool SaveToSlot(string slotPath, string stateJson)
    {
        EnsureInitialized();
        if (manager is null)
        {
            return false;
        }

        try
        {
            ApplyStateJson(stateJson);
            _ = manager.SaveGameToSlotAsync(slotPath).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool LoadSlot(string slotPath)
    {
        EnsureInitialized();
        if (manager is null)
        {
            return false;
        }

        try
        {
            _ = manager.LoadGameAsync(slotPath).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SaveAutoSaveSlot()
    {
        EnsureInitialized();
        if (manager is null)
        {
            return false;
        }

        try
        {
            _ = manager.SaveAutoSaveSlotAsync().GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool HandleDayStartAutoSave(int dayNumber)
    {
        EnsureInitialized();
        if (manager is null)
        {
            return false;
        }

        return manager.HandleDayStartAutoSaveAsync(dayNumber).GetAwaiter().GetResult();
    }

    public string[] GetObservedWriteKeys()
    {
        return writeKeys.ToArray();
    }

    public bool SlotExists(string slotPath)
    {
        EnsureInitialized();
        return store?.LoadSync(slotPath) is not null;
    }

    public string LoadRaw(string slotPath)
    {
        EnsureInitialized();
        return store?.LoadSync(slotPath) ?? string.Empty;
    }

    public bool SaveRaw(string slotPath, string payload)
    {
        EnsureInitialized();
        if (store is null)
        {
            return false;
        }

        try
        {
            store.SaveSync(slotPath, payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteSlot(string slotPath)
    {
        EnsureInitialized();
        if (store is null)
        {
            return false;
        }

        try
        {
            store.DeleteSync(slotPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void AdvanceRuntime(double deltaSeconds, bool isActiveUpdate = true)
    {
        EnsureInitialized();
        manager?.UpdateDayNightRuntime(deltaSeconds, isActiveUpdate);
    }

    public void SimulateNextSaveIoFailure()
    {
        EnsureInitialized();
        if (trackingStore is not null)
        {
            trackingStore.FailNextSave = true;
        }
    }

    public void SimulateNextLoadIoFailure()
    {
        EnsureInitialized();
        if (trackingStore is not null)
        {
            trackingStore.FailNextLoad = true;
        }
    }

    public bool LoadWithFeedback(string slotPath)
    {
        EnsureInitialized();
        if (manager is null || feedback is null)
        {
            return false;
        }

        var result = manager.TryLoadWithFeedbackAsync(slotPath, feedback).GetAwaiter().GetResult();
        lastLoadOk = result.ok;
        lastLoadReasonCode = result.reasonCode;
        return lastLoadOk;
    }

    public string LastLoadReasonCode()
    {
        return lastLoadReasonCode;
    }

    public bool LastLoadSucceeded()
    {
        return lastLoadOk;
    }

    public string LastFeedbackMessageKey()
    {
        EnsureInitialized();
        if (feedback is null || feedback.Events.Count == 0)
        {
            return string.Empty;
        }

        return feedback.Events[^1].MessageKey;
    }

    public string SnapshotStateJson()
    {
        EnsureInitialized();
        if (manager is null)
        {
            return "{}";
        }

        var state = manager.GetState();
        if (state is null)
        {
            return "{}";
        }

        var payload = new Dictionary<string, object?>
        {
            ["id"] = state.Id,
            ["level"] = state.Level,
            ["score"] = state.Score,
            ["health"] = state.Health,
            ["inventory"] = state.Inventory,
            ["x"] = state.Position.X,
            ["y"] = state.Position.Y
        };
        return JsonSerializer.Serialize(payload);
    }

    public int CurrentDay() => manager?.CurrentDayNightDay ?? -1;
    public string CurrentPhase() => manager?.CurrentDayNightPhase.ToString() ?? string.Empty;
    public long CurrentTick() => manager?.CurrentDayNightTick ?? -1;
    public double CurrentPhaseElapsedSeconds() => manager?.CurrentDayNightPhaseElapsedSeconds ?? -1d;

    private void EnsureInitialized()
    {
        if (manager is null || store is null || feedback is null)
        {
            ResetRuntime();
        }
    }

    private void ApplyStateJson(string stateJson)
    {
        if (manager is null || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stateJson)
                      ?? new Dictionary<string, JsonElement>();
        var current = manager.GetState() ?? new GameState(
            Id: "task25-fallback-state",
            Level: 1,
            Score: 0,
            Health: 100,
            Inventory: Array.Empty<string>(),
            Position: new Position(0, 0),
            Timestamp: DateTime.UtcNow);
        var cfg = manager.GetConfig() ?? new GameConfig(99, 100, 1.0, true, Difficulty.Medium);

        var id = ReadString(payload, "id") ?? current.Id;
        var level = ReadInt(payload, "level") ?? current.Level;
        var score = ReadInt(payload, "score") ?? current.Score;
        var health = ReadInt(payload, "health") ?? current.Health;
        var inv = ReadStringArray(payload, "inventory") ?? current.Inventory;
        var x = ReadDouble(payload, "x") ?? current.Position.X;
        var y = ReadDouble(payload, "y") ?? current.Position.Y;

        manager.SetState(
            new GameState(
                Id: id,
                Level: level,
                Score: score,
                Health: health,
                Inventory: inv,
                Position: new Position(x, y),
                Timestamp: DateTime.UtcNow),
            cfg);
    }

    private static string? ReadString(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private static int? ReadInt(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            return number;
        }

        return null;
    }

    private static double? ReadDouble(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number))
        {
            return number;
        }

        return null;
    }

    private static IReadOnlyList<string>? ReadStringArray(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } value)
            {
                list.Add(value);
            }
        }

        return list;
    }

    private sealed class TrackingStore(DataStoreAdapter inner, List<string> writeKeys) : Game.Core.Ports.IDataStore
    {
        public bool FailNextSave { get; set; }
        public bool FailNextLoad { get; set; }

        public System.Threading.Tasks.Task SaveAsync(string key, string json)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new InvalidOperationException("simulated_save_io_failure");
            }

            writeKeys.Add(key);
            return inner.SaveAsync(key, json);
        }

        public System.Threading.Tasks.Task<string?> LoadAsync(string key)
        {
            if (FailNextLoad)
            {
                FailNextLoad = false;
                throw new InvalidOperationException("simulated_load_io_failure");
            }

            return inner.LoadAsync(key);
        }

        public System.Threading.Tasks.Task DeleteAsync(string key)
        {
            return inner.DeleteAsync(key);
        }
    }
}
