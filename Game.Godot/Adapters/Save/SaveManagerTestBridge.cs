using System;
using System.Collections.Generic;
using System.Text.Json;
using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Services;
using Game.Core.State;
using Godot;
using GodotArray = Godot.Collections.Array<string>;
using GodotDictionary = Godot.Collections.Dictionary;

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
    private SaveManagerCloudSyncWorkflow? cloudWorkflow;
    private readonly Dictionary<string, string> cloudPayloadByAccountSlot = [];
    private readonly Dictionary<string, string> slotOwners = [];
    private readonly Dictionary<string, string> slotMetadataByAccountSlot = [];
    private readonly List<string> cloudOperationIds = [];
    private string cloudBackend = "STEAM_REMOTE_STORAGE_REAL";
    private bool cloudLoggedIn = true;
    private bool cloudRequireRealApiEvidence;
    private string cloudActiveAccountId = "steam_test_default";
    private string lastCloudStatusCode = "not_run";
    private string lastCloudStatusMessage = "";
    private string lastCloudOperationId = "";
    private string lastCloudEvidenceSource = "not_checked";

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
        ResetRuntimeCore(
            storageKey: storageKey,
            enableCompression: enableCompression,
            dayNightSeed: dayNightSeed,
            dayDurationSeconds: dayDurationSeconds,
            nightDurationSeconds: nightDurationSeconds,
            maxDay: maxDay,
            preserveCloudState: false);
    }

    public void ResetRuntimeKeepCloudState(
        string storageKey = StorageKey,
        bool enableCompression = false,
        int dayNightSeed = 20250425,
        int dayDurationSeconds = 10,
        int nightDurationSeconds = 10,
        int maxDay = 15)
    {
        ResetRuntimeCore(
            storageKey: storageKey,
            enableCompression: enableCompression,
            dayNightSeed: dayNightSeed,
            dayDurationSeconds: dayDurationSeconds,
            nightDurationSeconds: nightDurationSeconds,
            maxDay: maxDay,
            preserveCloudState: true);
    }

    private void ResetRuntimeCore(
        string storageKey,
        bool enableCompression,
        int dayNightSeed,
        int dayDurationSeconds,
        int nightDurationSeconds,
        int maxDay,
        bool preserveCloudState)
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
            dayNightConfig: new DayNightCycleConfig(dayDurationSeconds, nightDurationSeconds, maxDay),
            cloudSaveSyncService: new BridgeCloudSyncGateway(this));

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
        cloudWorkflow = new SaveManagerCloudSyncWorkflow(new BridgeCloudSyncGateway(this));
        if (!preserveCloudState)
        {
            cloudPayloadByAccountSlot.Clear();
            slotMetadataByAccountSlot.Clear();
            slotOwners.Clear();
            cloudOperationIds.Clear();
            lastCloudStatusCode = "not_run";
            lastCloudStatusMessage = string.Empty;
            lastCloudOperationId = string.Empty;
            cloudRequireRealApiEvidence = false;
            lastCloudEvidenceSource = "not_checked";
        }
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

    public void ResetCloudRuntime(string backend = "STEAM_REMOTE_STORAGE_REAL", bool loggedIn = true, string accountId = "steam_test_default", bool requireRealApiEvidence = false)
    {
        cloudBackend = string.IsNullOrWhiteSpace(backend) ? "STEAM_REMOTE_STORAGE_REAL" : backend.Trim();
        cloudLoggedIn = loggedIn;
        cloudActiveAccountId = string.IsNullOrWhiteSpace(accountId) ? "steam_test_default" : accountId.Trim();
        cloudRequireRealApiEvidence = requireRealApiEvidence;
        cloudPayloadByAccountSlot.Clear();
        slotMetadataByAccountSlot.Clear();
        slotOwners.Clear();
        cloudOperationIds.Clear();
        lastCloudStatusCode = "not_run";
        lastCloudStatusMessage = string.Empty;
        lastCloudOperationId = string.Empty;
        lastCloudEvidenceSource = "not_checked";
        manager?.ConfigureCloudSyncContext(false, cloudActiveAccountId);
    }

    public void ResetCloudRuntime(string backend, bool loggedIn, string accountId)
    {
        ResetCloudRuntime(backend, loggedIn, accountId, false);
    }

    public GodotDictionary SaveWithCloudSync(string slotPath, string requesterAccountId, string stateJson, bool cloudEnabled = true)
    {
        EnsureInitialized();
        EnsureCloudWorkflow();

        var requester = (requesterAccountId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(requester))
        {
            lastCloudStatusCode = "invalid_account";
            lastCloudStatusMessage = "Missing requester account id.";
            return BuildCloudResult(ok: false, uploaded: false, rejected: true, loadedFrom: "none", reasonCode: lastCloudStatusCode);
        }

        if (slotOwners.TryGetValue(slotPath, out var owner) && !string.Equals(owner, requester, StringComparison.Ordinal))
        {
            lastCloudStatusCode = "ownership_mismatch";
            lastCloudStatusMessage = "Slot owner mismatch.";
            lastCloudOperationId = string.Empty;
            lastCloudEvidenceSource = "ownership_binding_check";
            return BuildCloudResult(ok: false, uploaded: false, rejected: true, loadedFrom: "none", reasonCode: lastCloudStatusCode);
        }

        if (!cloudEnabled)
        {
            var localSaved = SaveToSlot(slotPath, stateJson);
            if (!localSaved)
            {
                lastCloudStatusCode = "local_save_failed";
                lastCloudStatusMessage = "Local save failed with cloud sync disabled.";
                return BuildCloudResult(ok: false, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
            }

            slotOwners[slotPath] = requester;
            lastCloudStatusCode = "cloud_disabled";
            lastCloudStatusMessage = "Cloud sync is disabled for this call.";
            return BuildCloudResult(ok: true, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        if (cloudWorkflow is null)
        {
            lastCloudStatusCode = "cloud_workflow_missing";
            lastCloudStatusMessage = "Cloud workflow is not initialized.";
            return BuildCloudResult(ok: false, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        var sync = manager?.SyncCloudUpload(slotPath, requester, stateJson, enableCloudSync: true);
        if (sync is null || !sync.Success)
        {
            return BuildCloudResult(ok: false, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        var localSavedAfterCloud = SaveToSlot(slotPath, stateJson);
        if (!localSavedAfterCloud)
        {
            lastCloudStatusCode = "local_save_failed";
            lastCloudStatusMessage = "Local save failed after successful cloud sync.";
            return BuildCloudResult(ok: false, uploaded: true, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        slotOwners[slotPath] = requester;

        var key = AccountSlotKey(requester, slotPath);
        cloudPayloadByAccountSlot[key] = stateJson;
        var metadata = BuildDeterministicMetadata(stateJson);
        slotMetadataByAccountSlot[key] = metadata;
        return BuildCloudResult(ok: true, uploaded: true, rejected: false, loadedFrom: "local", reasonCode: "ok");
    }

    public GodotDictionary LoadWithCloudSync(string slotPath, string requesterAccountId, bool cloudEnabled = true)
    {
        EnsureInitialized();
        EnsureCloudWorkflow();

        var requester = (requesterAccountId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(requester))
        {
            lastCloudStatusCode = "invalid_account";
            lastCloudStatusMessage = "Missing requester account id.";
            return BuildCloudResult(ok: false, uploaded: false, rejected: false, loadedFrom: "none", reasonCode: lastCloudStatusCode);
        }

        if (slotOwners.TryGetValue(slotPath, out var owner) && !string.Equals(owner, requester, StringComparison.Ordinal))
        {
            lastCloudStatusCode = "ownership_mismatch";
            lastCloudStatusMessage = "Slot owner mismatch.";
            lastCloudOperationId = string.Empty;
            lastCloudEvidenceSource = "ownership_binding_check";
            return BuildCloudResult(ok: false, uploaded: false, rejected: true, loadedFrom: "none", reasonCode: lastCloudStatusCode);
        }

        if (!cloudEnabled)
        {
            var localLoaded = LoadSlot(slotPath);
            lastCloudStatusCode = "cloud_disabled";
            lastCloudStatusMessage = "Cloud sync is disabled for this call.";
            return BuildCloudResult(ok: localLoaded, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        var download = manager?.SyncCloudDownload(slotPath, requester, enableCloudSync: true);
        if (download is null || !download.Success)
        {
            var fallbackLoaded = LoadSlot(slotPath);
            return BuildCloudResult(ok: fallbackLoaded, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        var key = AccountSlotKey(requester, slotPath);
        if (!cloudPayloadByAccountSlot.TryGetValue(key, out var cloudPayload))
        {
            var localLoaded = LoadSlot(slotPath);
            lastCloudStatusCode = "cloud_not_found";
            lastCloudStatusMessage = "Cloud payload not found.";
            return BuildCloudResult(ok: localLoaded, uploaded: false, rejected: false, loadedFrom: "local", reasonCode: lastCloudStatusCode);
        }

        var metadataKey = AccountSlotKey(requester, slotPath);
        var cloudMetadata = BuildDeterministicMetadata(cloudPayload);
        if (slotMetadataByAccountSlot.TryGetValue(metadataKey, out var expectedMetadata)
            && !string.Equals(expectedMetadata, cloudMetadata, StringComparison.Ordinal))
        {
            lastCloudStatusCode = "metadata_mismatch";
            lastCloudStatusMessage = "Deterministic metadata mismatch between local and cloud binding.";
            lastCloudEvidenceSource = "metadata_binding_check";
            return BuildCloudResult(ok: false, uploaded: false, rejected: true, loadedFrom: "none", reasonCode: lastCloudStatusCode);
        }

        var restoreOk = SaveToSlot(slotPath, cloudPayload);
        var loaded = restoreOk && LoadSlot(slotPath);
        return BuildCloudResult(ok: loaded, uploaded: false, rejected: false, loadedFrom: "cloud", reasonCode: loaded ? "ok" : "cloud_restore_failed");
    }

    public void InjectCloudPayloadForTesting(string slotPath, string accountId, string payload, bool updateMetadata = false)
    {
        var key = AccountSlotKey(accountId, slotPath);
        cloudPayloadByAccountSlot[key] = payload;
        if (updateMetadata)
        {
            slotMetadataByAccountSlot[key] = BuildDeterministicMetadata(payload);
        }
    }

    public GodotDictionary ResolveCloudConflict(string localRevision, string localPayload, string cloudRevision, string cloudPayload, string choice)
    {
        var resolver = new CloudSaveConflictResolver();
        var selected = CloudConflictChoice.None;
        if (string.Equals(choice, "local", StringComparison.OrdinalIgnoreCase))
        {
            selected = CloudConflictChoice.Local;
        }
        else if (string.Equals(choice, "cloud", StringComparison.OrdinalIgnoreCase))
        {
            selected = CloudConflictChoice.Cloud;
        }

        var result = resolver.Resolve(
            new CloudSaveSnapshot(localRevision ?? string.Empty, localPayload ?? string.Empty),
            new CloudSaveSnapshot(cloudRevision ?? string.Empty, cloudPayload ?? string.Empty),
            selected);

        var resolvedPayload = string.Empty;
        var resolvedRevision = string.Empty;
        if (selected == CloudConflictChoice.Local)
        {
            resolvedPayload = localPayload ?? string.Empty;
            resolvedRevision = localRevision ?? string.Empty;
        }
        else if (selected == CloudConflictChoice.Cloud)
        {
            resolvedPayload = cloudPayload ?? string.Empty;
            resolvedRevision = cloudRevision ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(resolvedPayload))
        {
            ApplyStateJson(resolvedPayload);
        }

        return new GodotDictionary
        {
            ["prompt_required"] = result.RequiresUserDecision,
            ["applied_local"] = result.AppliedLocalForThisOperation,
            ["applied_cloud"] = result.AppliedCloudForThisOperation,
            ["cloud_overwrite_scheduled"] = result.CloudOverwriteScheduled,
            ["resolved_payload"] = resolvedPayload,
            ["resolved_revision"] = resolvedRevision,
        };
    }

    public string LastCloudStatusCode() => lastCloudStatusCode;

    public string LastCloudStatusMessage() => lastCloudStatusMessage;

    public string LastCloudOperationId() => lastCloudOperationId;
    public string LastCloudEvidenceSource() => lastCloudEvidenceSource;

    public GodotArray GetCloudOperationIds()
    {
        var list = new GodotArray();
        foreach (var item in cloudOperationIds)
        {
            list.Add(item);
        }
        return list;
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

    private void EnsureCloudWorkflow()
    {
        cloudWorkflow ??= new SaveManagerCloudSyncWorkflow(new BridgeCloudSyncGateway(this));
    }

    private string AccountSlotKey(string accountId, string slotPath)
    {
        return $"{accountId}::{slotPath}";
    }

    private static string BuildDeterministicMetadata(string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return "empty";
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stateJson)
                          ?? new Dictionary<string, JsonElement>();
            var level = ReadInt(payload, "level") ?? ReadInt(payload, "Level") ?? 0;
            var score = ReadInt(payload, "score") ?? ReadInt(payload, "Score") ?? 0;
            var health = ReadInt(payload, "health") ?? ReadInt(payload, "Health") ?? 0;
            return $"lvl={level}|score={score}|hp={health}";
        }
        catch
        {
            return "invalid-json";
        }
    }

    private GodotDictionary BuildCloudResult(bool ok, bool uploaded, bool rejected, string loadedFrom, string reasonCode)
    {
        var result = new GodotDictionary
        {
            ["ok"] = ok,
            ["uploaded"] = uploaded,
            ["rejected"] = rejected,
            ["loaded_from"] = loadedFrom,
            ["reason_code"] = reasonCode ?? string.Empty,
            ["status_message"] = lastCloudStatusMessage,
            ["operation_id"] = lastCloudOperationId,
            ["backend"] = cloudBackend,
            ["evidence_source"] = lastCloudEvidenceSource,
            ["real_api_checked"] = string.Equals(lastCloudEvidenceSource, "steam_remote_storage_methods", StringComparison.Ordinal),
        };
        return result;
    }

    private CloudSaveSyncResultDto ExecuteCloudSync(string slotId, string steamAccountId, string direction)
    {
        var opId = $"steam-{direction}-{cloudOperationIds.Count + 1}";
        cloudOperationIds.Add(opId);
        lastCloudOperationId = opId;

        if (!cloudLoggedIn)
        {
            lastCloudStatusCode = "steam_login_required";
            lastCloudStatusMessage = "No valid Steam login was found.";
            return new CloudSaveSyncResultDto(slotId, direction, false, lastCloudStatusCode, string.Empty, DateTimeOffset.UtcNow);
        }

        if (!string.Equals(cloudBackend, "STEAM_REMOTE_STORAGE_REAL", StringComparison.OrdinalIgnoreCase))
        {
            lastCloudEvidenceSource = "backend_not_remote_storage";
            lastCloudStatusCode = "steam_remote_storage_required";
            lastCloudStatusMessage = "Current backend is not Steam Remote Storage.";
            return new CloudSaveSyncResultDto(slotId, direction, false, lastCloudStatusCode, string.Empty, DateTimeOffset.UtcNow);
        }

        var hasSteamRemoteStorageEvidence = TryProbeSteamRemoteStorageCapabilities(out var evidenceReason);
        if (!hasSteamRemoteStorageEvidence && cloudRequireRealApiEvidence)
        {
            lastCloudStatusCode = "steam_api_unavailable";
            lastCloudStatusMessage = evidenceReason;
            return new CloudSaveSyncResultDto(slotId, direction, false, lastCloudStatusCode, string.Empty, DateTimeOffset.UtcNow);
        }

        if (!string.Equals(steamAccountId, cloudActiveAccountId, StringComparison.Ordinal))
        {
            lastCloudEvidenceSource = hasSteamRemoteStorageEvidence ? "steam_remote_storage_methods" : lastCloudEvidenceSource;
            lastCloudStatusCode = "steam_account_mismatch";
            lastCloudStatusMessage = "Requester account does not match active Steam account.";
            return new CloudSaveSyncResultDto(slotId, direction, false, lastCloudStatusCode, string.Empty, DateTimeOffset.UtcNow);
        }

        lastCloudEvidenceSource = hasSteamRemoteStorageEvidence ? "steam_remote_storage_methods" : lastCloudEvidenceSource;
        lastCloudStatusCode = "ok";
        lastCloudStatusMessage = "Steam cloud sync completed.";
        var revision = $"{direction}-rev-{cloudOperationIds.Count}";
        return new CloudSaveSyncResultDto(slotId, direction, true, string.Empty, revision, DateTimeOffset.UtcNow);
    }

    private bool TryProbeSteamRemoteStorageCapabilities(out string reason)
    {
        if (!Engine.HasSingleton("Steam"))
        {
            lastCloudEvidenceSource = "steam_singleton_missing";
            reason = "Steam singleton is not available in current runtime.";
            return false;
        }

        var steam = Engine.GetSingleton("Steam");
        if (steam is null)
        {
            lastCloudEvidenceSource = "steam_singleton_null";
            reason = "Steam singleton is null.";
            return false;
        }

        var hasRead = steam.HasMethod("FileRead") || steam.HasMethod("fileRead");
        var hasWrite = steam.HasMethod("FileWrite") || steam.HasMethod("fileWrite");
        if (hasRead && hasWrite)
        {
            lastCloudEvidenceSource = "steam_remote_storage_methods";
            reason = string.Empty;
            return true;
        }

        lastCloudEvidenceSource = "steam_singleton_without_remote_storage_methods";
        reason = "Steam singleton does not expose Remote Storage FileRead/FileWrite methods.";
        return false;
    }

    private sealed class BridgeCloudSyncGateway : ICloudSaveSyncService
    {
        private readonly SaveManagerTestBridge owner;

        public BridgeCloudSyncGateway(SaveManagerTestBridge owner)
        {
            this.owner = owner;
        }

        public CloudSaveSyncResultDto Sync(string runId, string slotId, string direction, string steamAccountId, string payload)
        {
            return owner.ExecuteCloudSync(slotId, steamAccountId, direction);
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
