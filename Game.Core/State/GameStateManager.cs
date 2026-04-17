using System.Text.Json;
using Game.Core.Contracts;
using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;
using Game.Core.Domain;
using Game.Core.Ports;
using Game.Core.Services;

namespace Game.Core.State;

public class GameStateManager
{
    public const string CurrentSaveVersion = "1.0.0";
    public const string DefaultAutoSaveSlotPath = "user://autosave.save";

    private readonly GameStateManagerOptions _options;
    private readonly IDataStore _store;
    private readonly DayNightRuntimeStateMachine _dayNightRuntime;
    private readonly ICloudSaveSyncService? _cloudSaveSyncService;
    private readonly SaveManagerCloudSyncWorkflow? _cloudSaveSyncWorkflow;
    private bool _cloudSyncEnabled;
    private string _cloudSyncSteamAccountId = string.Empty;
    private readonly List<Action<DomainEvent>> _callbacks = new();

    private GameState? _currentState;
    private GameConfig? _currentConfig;
    private bool _autoSaveEnabled;
    private bool _runTerminal;
    private RunTerminalOutcome _runTerminalOutcome = RunTerminalOutcome.None;
    private bool _winPresentationVisible;
    private int _castleHp = 100;
    private RunTerminalState? _lastRunTerminalState;
    private int _lastDayStartAutoSaveDay;

    private const string IndexSuffix = ":index";

    public GameStateManager(
        IDataStore store,
        GameStateManagerOptions? options = null,
        int dayNightSeed = 0,
        DayNightCycleConfig? dayNightConfig = null,
        ICloudSaveSyncService? cloudSaveSyncService = null)
    {
        _store = store;
        _options = options ?? GameStateManagerOptions.Default;
        _dayNightRuntime = new DayNightRuntimeStateMachine(dayNightSeed, dayNightConfig);
        _dayNightRuntime.OnCheckpoint += HandleDayNightCheckpoint;
        _dayNightRuntime.OnTerminal += HandleDayNightTerminal;
        _cloudSaveSyncService = cloudSaveSyncService;
        if (_cloudSaveSyncService is not null)
        {
            _cloudSaveSyncWorkflow = new SaveManagerCloudSyncWorkflow(_cloudSaveSyncService);
        }
    }

    public DayNightPhase CurrentDayNightPhase => _dayNightRuntime.CurrentPhase;
    public int CurrentDayNightDay => _dayNightRuntime.CurrentDay;
    public int DayNightCheckpointCount => _dayNightRuntime.CheckpointCount;
    public long CurrentDayNightTick => _dayNightRuntime.Tick;
    public double CurrentDayNightPhaseElapsedSeconds => _dayNightRuntime.PhaseElapsedSeconds;
    public bool IsRunTerminal => _runTerminal;
    public RunTerminalOutcome CurrentRunTerminalOutcome => _runTerminalOutcome;
    public int CurrentCastleHp => _castleHp;
    public bool IsWinPresentationVisible => _winPresentationVisible;
    public RunTerminalState? LastRunTerminalState => _lastRunTerminalState;
    public event Action<DayNightCheckpoint>? OnDayNightCheckpoint;
    public event Action<DayNightTerminal>? OnDayNightTerminal;
    public event Action<RunTerminalState>? OnRunTerminal;

    public void SetState(GameState state, GameConfig? config = null)
    {
        _currentState = state with { };
        _castleHp = Math.Max(0, state.Health);
        if (config is not null)
            _currentConfig = config with { };

        Publish(DomainEvent.Create(
            type: "game.state.manager.updated",
            source: nameof(GameStateManager),
            payload: new StateUpdatedPayload(state, config),
            timestamp: DateTime.UtcNow,
            id: $"state-update-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));
    }

    public GameState? GetState() => _currentState is null ? null : _currentState with { };
    public GameConfig? GetConfig() => _currentConfig is null ? null : _currentConfig with { };

    public void UpdateDayNightRuntime(double deltaSeconds, bool isActiveUpdate = true)
    {
        if (_runTerminal)
        {
            return;
        }

        _dayNightRuntime.Update(deltaSeconds, isActiveUpdate);
        EvaluateRunTerminalConditions();
    }

    public bool ForceDayNightTerminal()
    {
        if (_runTerminal)
        {
            return false;
        }

        var forced = _dayNightRuntime.ForceTerminal();
        if (forced)
        {
            var outcome = _castleHp <= 0 ? RunTerminalOutcome.Loss : RunTerminalOutcome.Win;
            EnterRunTerminal(outcome, reason: "forced-terminal", forceDayNightTerminal: false);
        }

        return forced;
    }

    public bool RequestDayNightTransition(DayNightPhase requestedPhase)
    {
        return _dayNightRuntime.RequestTransition(requestedPhase);
    }

    public void SetCastleHp(int currentHp)
    {
        var normalized = Math.Max(0, currentHp);
        var previous = _castleHp;
        _castleHp = normalized;

        if (_currentState is not null && _currentState.Health != normalized)
        {
            _currentState = _currentState with
            {
                Health = normalized,
                Timestamp = DateTime.UtcNow,
            };
        }

        if (previous != normalized)
        {
            Publish(DomainEvent.Create(
                type: EventTypes.LastkingCastleHpChanged,
                source: nameof(GameStateManager),
                payload: new CastleHpChangedPayload(
                    Day: _dayNightRuntime.CurrentDay,
                    PreviousHp: previous,
                    CurrentHp: normalized),
                timestamp: DateTime.UtcNow,
                id: $"castle-hp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            ));
        }

        EvaluateRunTerminalConditions();
    }

    public void ApplyCastleDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        SetCastleHp(_castleHp - damage);
    }

    public bool RestartRun(int? startingCastleHp = null)
    {
        _runTerminal = false;
        _runTerminalOutcome = RunTerminalOutcome.None;
        _winPresentationVisible = false;
        _lastRunTerminalState = null;
        _dayNightRuntime.Reset();
        _lastDayStartAutoSaveDay = 0;

        if (startingCastleHp is not null)
        {
            SetCastleHp(startingCastleHp.Value);
            return true;
        }

        if (_currentState is not null)
        {
            SetCastleHp(_currentState.Health);
        }

        return true;
    }

    private const int MaxTitleLength = 100;
    private const int MaxScreenshotChars = 2_000_000; // ~>1.5MB base64

    public async Task<string> SaveGameAsync(string? name = null, string? screenshot = null)
    {
        var saveId = $"{_options.StorageKey}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return await SaveGameToSlotAsync(saveId, name, screenshot);
    }

    public async Task<string> SaveGameToSlotAsync(string saveId, string? name = null, string? screenshot = null)
    {
        if (_currentState is null || _currentConfig is null)
            throw new InvalidOperationException("No game state to save");
        if (string.IsNullOrWhiteSpace(saveId))
            throw new ArgumentException("Save slot id must be provided.", nameof(saveId));

        if (!string.IsNullOrEmpty(name) && name!.Length > MaxTitleLength)
            throw new ArgumentOutOfRangeException(nameof(name), $"Title too long (>{MaxTitleLength}).");
        if (!string.IsNullOrEmpty(screenshot) && screenshot!.Length > MaxScreenshotChars)
            throw new ArgumentOutOfRangeException(nameof(screenshot), $"Screenshot too large (>{MaxScreenshotChars} chars).");
        var checksum = CalculateChecksum(_currentState);
        var now = DateTime.UtcNow;

        var save = new SaveData(
            Id: saveId,
            State: _currentState,
            Config: _currentConfig,
            Metadata: new SaveMetadata(now, now, CurrentSaveVersion, checksum),
            Screenshot: screenshot,
            Title: name,
            DayNightRuntime: _dayNightRuntime.ExportSnapshot()
        );

        await SaveToStoreAsync(saveId, save);
        await UpdateIndexAsync(add: saveId);
        await CleanupOldSavesAsync();
        TryCloudSyncUpload(saveId, save);

        Publish(DomainEvent.Create(
            type: "game.save.created",
            source: nameof(GameStateManager),
            payload: new SaveRefPayload(saveId),
            timestamp: now,
            id: $"save-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));

        return saveId;
    }

    public async Task<string> SaveAutoSaveSlotAsync(string? screenshot = null)
    {
        return await SaveGameToSlotAsync(DefaultAutoSaveSlotPath, name: "autosave", screenshot: screenshot);
    }

    public CloudSaveSyncResultDto? SyncCloudUpload(string saveId, string steamAccountId, string payload, bool enableCloudSync = true)
    {
        if (_cloudSaveSyncWorkflow is null)
        {
            return null;
        }

        return _cloudSaveSyncWorkflow.Save(
            new SaveWorkflowCommand(
                SlotId: saveId,
                EnableCloudSync: enableCloudSync,
                SteamAccountId: steamAccountId,
                Payload: payload));
    }

    public CloudSaveSyncResultDto? SyncCloudDownload(string saveId, string steamAccountId, bool enableCloudSync = true)
    {
        if (!enableCloudSync || _cloudSaveSyncService is null || string.IsNullOrWhiteSpace(saveId) || string.IsNullOrWhiteSpace(steamAccountId))
        {
            return null;
        }

        var runId = $"task26-load-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return _cloudSaveSyncService.Sync(
            runId: runId,
            slotId: saveId,
            direction: "download",
            steamAccountId: steamAccountId,
            payload: string.Empty);
    }

    public void ConfigureCloudSyncContext(bool enabled, string steamAccountId)
    {
        _cloudSyncEnabled = enabled;
        _cloudSyncSteamAccountId = (steamAccountId ?? string.Empty).Trim();
    }

    public async Task<bool> HandleDayStartAutoSaveAsync(int dayNumber, string? screenshot = null)
    {
        if (dayNumber <= 0)
        {
            return false;
        }

        if (_lastDayStartAutoSaveDay == dayNumber)
        {
            return false;
        }

        _lastDayStartAutoSaveDay = dayNumber;
        await SaveAutoSaveSlotAsync(screenshot);

        Publish(DomainEvent.Create(
            type: EventTypes.LastkingSaveAutosaved,
            source: nameof(GameStateManager),
            payload: new AutoSaveDayStartedPayload(DefaultAutoSaveSlotPath, dayNumber),
            timestamp: DateTime.UtcNow,
            id: $"lastking-autosave-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));

        return true;
    }

    public async Task<(GameState state, GameConfig config)> LoadGameAsync(string saveId)
    {
        TryCloudSyncDownload(saveId);
        var save = await LoadFromStoreAsync(saveId);
        if (!string.Equals(save.Metadata.Version, CurrentSaveVersion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Save version incompatible with current runtime.");

        var checksum = CalculateChecksum(save.State);
        if (!string.Equals(checksum, save.Metadata.Checksum, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Save file is corrupted");

        _currentState = save.State with { };
        _currentConfig = save.Config with { };
        if (save.DayNightRuntime is not null)
        {
            _dayNightRuntime.RestoreSnapshot(save.DayNightRuntime);
        }

        Publish(DomainEvent.Create(
            type: "game.save.loaded",
            source: nameof(GameStateManager),
            payload: new SaveRefPayload(saveId),
            timestamp: DateTime.UtcNow,
            id: $"load-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));

        return (_currentState, _currentConfig);
    }

    private void TryCloudSyncUpload(string saveId, SaveData save)
    {
        if (!_cloudSyncEnabled || string.IsNullOrWhiteSpace(_cloudSyncSteamAccountId) || _cloudSaveSyncWorkflow is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(save);
        _ = _cloudSaveSyncWorkflow.Save(
            new SaveWorkflowCommand(
                SlotId: saveId,
                EnableCloudSync: true,
                SteamAccountId: _cloudSyncSteamAccountId,
                Payload: payload));
    }

    private void TryCloudSyncDownload(string saveId)
    {
        if (!_cloudSyncEnabled || string.IsNullOrWhiteSpace(_cloudSyncSteamAccountId) || _cloudSaveSyncService is null)
        {
            return;
        }

        var runId = $"task26-load-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        _ = _cloudSaveSyncService.Sync(
            runId: runId,
            slotId: saveId,
            direction: "download",
            steamAccountId: _cloudSyncSteamAccountId,
            payload: string.Empty);
    }

    public async Task<(bool ok, string reasonCode)> TryLoadWithFeedbackAsync(string saveId, UIFeedbackPipeline feedback)
    {
        if (feedback is null)
        {
            throw new ArgumentNullException(nameof(feedback));
        }

        var beforeState = _currentState is null ? null : _currentState with { };
        var beforeConfig = _currentConfig is null ? null : _currentConfig with { };
        var beforeRuntime = _dayNightRuntime.ExportSnapshot();

        try
        {
            _ = await LoadGameAsync(saveId);
            return (true, "ok");
        }
        catch (InvalidOperationException ex)
        {
            var message = ex.Message ?? string.Empty;
            string reasonCode;
            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                reasonCode = "missing_autosave";
                feedback.ReportLoadFailure(reasonCode, saveId);
            }
            else if (message.Contains("version", StringComparison.OrdinalIgnoreCase))
            {
                reasonCode = "migration_version_incompatible";
                feedback.ReportMigrationFailure(reasonCode, saveId);
            }
            else if (message.Contains("deserialize", StringComparison.OrdinalIgnoreCase) ||
                     message.Contains("format", StringComparison.OrdinalIgnoreCase))
            {
                reasonCode = "deserialize_failed";
                feedback.ReportLoadFailure(reasonCode, saveId);
            }
            else
            {
                reasonCode = "invalid_content";
                feedback.ReportLoadFailure(reasonCode, saveId);
            }

            RestoreRuntimeSnapshot(beforeState, beforeConfig, beforeRuntime);
            return (false, reasonCode);
        }
    }

    public async Task DeleteSaveAsync(string saveId)
    {
        await _store.DeleteAsync(saveId);
        await UpdateIndexAsync(remove: saveId);

        Publish(DomainEvent.Create(
            type: "game.save.deleted",
            source: nameof(GameStateManager),
            payload: new SaveRefPayload(saveId),
            timestamp: DateTime.UtcNow,
            id: $"delete-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));
    }

    public async Task<IReadOnlyList<SaveData>> GetSaveListAsync()
    {
        var ids = await ReadIndexAsync();
        var list = new List<SaveData>();
        foreach (var id in ids)
        {
            try { list.Add(await LoadFromStoreAsync(id)); }
            catch { /* ignore broken entries */ }
        }
        return list
            .OrderByDescending(s => s.Metadata.CreatedAt)
            .ToList();
    }

    public void EnableAutoSave()
    {
        if (_autoSaveEnabled) return;
        _autoSaveEnabled = true;
        Publish(DomainEvent.Create(
            type: "game.autosave.enabled",
            source: nameof(GameStateManager),
            payload: new AutoSaveIntervalPayload(_options.AutoSaveInterval.TotalMilliseconds),
            timestamp: DateTime.UtcNow,
            id: $"autosave-enable-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));
    }

    public void DisableAutoSave()
    {
        if (!_autoSaveEnabled) return;
        _autoSaveEnabled = false;
        Publish(DomainEvent.Create(
            type: "game.autosave.disabled",
            source: nameof(GameStateManager),
            payload: EmptyPayload.Instance,
            timestamp: DateTime.UtcNow,
            id: $"autosave-disable-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));
    }

    // For tests or scheduler to trigger
    public async Task AutoSaveTickAsync()
    {
        if (_autoSaveEnabled && _currentState is not null && _currentConfig is not null)
        {
            await SaveGameAsync($"auto-save-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            Publish(DomainEvent.Create(
                type: "game.autosave.completed",
                source: nameof(GameStateManager),
                payload: new AutoSaveIntervalPayload(_options.AutoSaveInterval.TotalMilliseconds),
                timestamp: DateTime.UtcNow,
                id: $"autosave-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            ));
        }
    }

    public void OnEvent(Action<DomainEvent> callback) => _callbacks.Add(callback);
    public void OffEvent(Action<DomainEvent> callback) => _callbacks.Remove(callback);

    public void Destroy()
    {
        _callbacks.Clear();
        _currentState = null;
        _currentConfig = null;
        _autoSaveEnabled = false;
    }

    private async Task SaveToStoreAsync(string key, SaveData data)
    {
        var json = JsonSerializer.Serialize(data);
        if (_options.EnableCompression)
        {
            var compressed = CompressToBase64(json);
            await _store.SaveAsync(key, "gz:" + compressed);
        }
        else
        {
            await _store.SaveAsync(key, json);
        }
    }

    private async Task<SaveData> LoadFromStoreAsync(string key)
    {
        var raw = await _store.LoadAsync(key) ?? throw new InvalidOperationException($"Save not found: {key}");
        var isCompressedPayload = raw.StartsWith("gz:", StringComparison.Ordinal);
        if (_options.EnableCompression != isCompressedPayload)
        {
            throw new InvalidOperationException("Save format mismatch with current serialization format.");
        }

        string json;
        if (isCompressedPayload)
        {
            try
            {
                json = DecompressFromBase64(raw.Substring(3));
            }
            catch (Exception ex) when (ex is FormatException or InvalidDataException)
            {
                throw new InvalidOperationException("Failed to deserialize save payload.", ex);
            }
        }
        else
        {
            json = raw;
        }

        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data is null)
            {
                throw new InvalidOperationException("Failed to deserialize save payload.");
            }

            return data;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize save payload.", ex);
        }
    }

    private void RestoreRuntimeSnapshot(GameState? state, GameConfig? config, DayNightRuntimeSnapshot runtime)
    {
        _currentState = state is null ? null : state with { };
        _currentConfig = config is null ? null : config with { };
        _dayNightRuntime.RestoreSnapshot(runtime);
    }

    private async Task UpdateIndexAsync(string? add = null, string? remove = null)
    {
        var key = _options.StorageKey + IndexSuffix;
        var json = await _store.LoadAsync(key);
        var ids = json is null ? new List<string>() : (JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>());
        if (add is not null) ids.Insert(0, add);
        if (remove is not null) ids.Remove(remove);
        var outJson = JsonSerializer.Serialize(ids.Distinct().ToList());
        await _store.SaveAsync(key, outJson);
    }

    private async Task CleanupOldSavesAsync()
    {
        var key = _options.StorageKey + IndexSuffix;
        var json = await _store.LoadAsync(key);
        if (json is null) return;
        var ids = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        if (ids.Count <= _options.MaxSaves) return;
        var toDelete = ids.Skip(_options.MaxSaves).ToList();
        foreach (var id in toDelete)
        {
            await _store.DeleteAsync(id);
            ids.Remove(id);
        }
        await _store.SaveAsync(key, JsonSerializer.Serialize(ids));
    }

    private async Task<List<string>> ReadIndexAsync()
    {
        var key = _options.StorageKey + IndexSuffix;
        var json = await _store.LoadAsync(key);
        return json is null ? new List<string>() : (JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>());
    }

    private static string CalculateChecksum(GameState state)
    {
        var json = JsonSerializer.Serialize(state);
        long hash = 0;
        foreach (var ch in json)
        {
            hash = ((hash << 5) - hash) + ch;
            hash &= 0xFFFFFFFF; // clamp to 32-bit
        }
        return hash.ToString("X");
    }

    private void Publish(DomainEvent evt)
    {
        foreach (var cb in _callbacks.ToArray())
        {
            try { cb(evt); } catch { /* ignore */ }
        }
    }

    private sealed record StateUpdatedPayload(GameState State, GameConfig? Config);
    private sealed record SaveRefPayload(string SaveId);
    private sealed record AutoSaveIntervalPayload(double IntervalMilliseconds);
    private sealed record AutoSaveDayStartedPayload(string SlotId, int DayNumber);
    private sealed record DayNightCheckpointPayload(int Day, string From, string To, long Tick, int RandomToken);
    private sealed record DayNightTerminalPayload(int Day, long Tick);
    private sealed record CastleHpChangedPayload(int Day, int PreviousHp, int CurrentHp);
    private sealed record RunTerminalPayload(string Outcome, int Day, int CastleHp, string EndHandling, string Reason);
    private sealed record UiFeedbackPayload(string Code, string MessageKey, string Severity, string Details);

    private sealed class EmptyPayload
    {
        private EmptyPayload()
        {
        }

        public static EmptyPayload Instance { get; } = new();
    }

    private static string CompressToBase64(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        using var ms = new MemoryStream();
        using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.SmallestSize, true))
        {
            gz.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string DecompressFromBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var ms = new MemoryStream(bytes);
        using var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        gz.CopyTo(outMs);
        return System.Text.Encoding.UTF8.GetString(outMs.ToArray());
    }

    private void HandleDayNightCheckpoint(DayNightCheckpoint checkpoint)
    {
        OnDayNightCheckpoint?.Invoke(checkpoint);
        var eventType = checkpoint.To == DayNightPhase.Day
            ? EventTypes.LastkingDayStarted
            : EventTypes.LastkingNightStarted;
        Publish(DomainEvent.Create(
            type: eventType,
            source: nameof(GameStateManager),
            payload: new DayNightCheckpointPayload(
                checkpoint.Day,
                checkpoint.From.ToString(),
                checkpoint.To.ToString(),
                checkpoint.Tick,
                checkpoint.RandomToken),
            timestamp: DateTime.UtcNow,
            id: $"day-night-checkpoint-{checkpoint.Tick}-{checkpoint.DayNightPhaseHash()}"
        ));
    }

    private void HandleDayNightTerminal(DayNightTerminal terminal)
    {
        OnDayNightTerminal?.Invoke(terminal);
        Publish(DomainEvent.Create(
            type: EventTypes.LastkingDayNightTerminal,
            source: nameof(GameStateManager),
            payload: new DayNightTerminalPayload(terminal.Day, terminal.Tick),
            timestamp: DateTime.UtcNow,
            id: $"day-night-terminal-{terminal.Tick}-{terminal.Day}"
        ));

        if (_runTerminal)
        {
            return;
        }

        EvaluateRunTerminalConditionsFromTerminal(terminal);
    }

    private void EvaluateRunTerminalConditions()
    {
        if (_runTerminal || _currentState is null)
        {
            return;
        }

        if (_castleHp <= 0)
        {
            EnterRunTerminal(RunTerminalOutcome.Loss, reason: "castle-hp-depleted", forceDayNightTerminal: true);
            return;
        }

    }

    private void EvaluateRunTerminalConditionsFromTerminal(DayNightTerminal terminal)
    {
        if (_currentState is null)
        {
            return;
        }

        if (_castleHp <= 0)
        {
            EnterRunTerminal(RunTerminalOutcome.Loss, reason: "terminal-while-castle-depleted", forceDayNightTerminal: false);
            return;
        }

        if (terminal.Day >= _dayNightRuntime.MaxDay &&
            terminal.FromPhase == DayNightPhase.Night)
        {
            EnterRunTerminal(RunTerminalOutcome.Win, reason: "terminal-max-day-reached", forceDayNightTerminal: false);
        }
    }

    private void EnterRunTerminal(RunTerminalOutcome outcome, string reason, bool forceDayNightTerminal)
    {
        if (_runTerminal)
        {
            return;
        }

        _runTerminal = true;
        _runTerminalOutcome = outcome;
        _winPresentationVisible = outcome == RunTerminalOutcome.Win;

        if (forceDayNightTerminal)
        {
            _dayNightRuntime.ForceTerminal();
        }

        var terminalState = new RunTerminalState(
            Outcome: outcome,
            Day: _dayNightRuntime.CurrentDay,
            CastleHp: _castleHp,
            AppliedHandling: _options.EndOfGameHandling,
            WinPresentationVisible: _winPresentationVisible,
            Tick: _dayNightRuntime.Tick);

        _lastRunTerminalState = terminalState;
        OnRunTerminal?.Invoke(terminalState);

        Publish(DomainEvent.Create(
            type: EventTypes.RunStateTransitioned,
            source: nameof(GameStateManager),
            payload: new RunTerminalPayload(
                Outcome: outcome.ToString(),
                Day: terminalState.Day,
                CastleHp: terminalState.CastleHp,
                EndHandling: terminalState.AppliedHandling.ToString(),
                Reason: reason),
            timestamp: DateTime.UtcNow,
            id: $"run-terminal-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));

        if (_options.EndOfGameHandling == EndOfGameHandling.Reset)
        {
            _dayNightRuntime.Reset();
        }

        if (outcome == RunTerminalOutcome.Win)
        {
            Publish(DomainEvent.Create(
                type: EventTypes.LastkingUiFeedbackRaised,
                source: nameof(GameStateManager),
                payload: new UiFeedbackPayload(
                    Code: "run.win.day15",
                    MessageKey: "ui.run.win.day15",
                    Severity: "info",
                    Details: "day15-survived-castle-intact"),
                timestamp: DateTime.UtcNow,
                id: $"ui-feedback-win-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            ));
        }
    }
}

file static class DayNightCheckpointExtensions
{
    public static int DayNightPhaseHash(this DayNightCheckpoint checkpoint)
    {
        unchecked
        {
            return ((int)checkpoint.From * 397) ^ (int)checkpoint.To;
        }
    }
}
