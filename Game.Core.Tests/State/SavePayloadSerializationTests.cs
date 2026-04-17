using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.State;

public class SavePayloadSerializationTests
{
    private sealed class InMemoryDataStore : IDataStore
    {
        private readonly Dictionary<string, string> items = new();

        public Task SaveAsync(string key, string json)
        {
            items[key] = json;
            return Task.CompletedTask;
        }

        public Task<string?> LoadAsync(string key)
        {
            items.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task DeleteAsync(string key)
        {
            items.Remove(key);
            return Task.CompletedTask;
        }
    }

    // ACC:T25.13
    [Fact]
    public async Task ShouldRestoreStateSeedAndWaveTimerExactly_WhenSaveThenLoadRoundTrips()
    {
        var store = new InMemoryDataStore();
        var options = new GameStateManagerOptions(StorageKey: "task25-roundtrip-autosave", EnableCompression: false);
        var cycleConfig = new DayNightCycleConfig(DayDurationSeconds: 10, NightDurationSeconds: 10, MaxDay: 15);

        var savingManager = new GameStateManager(store, options, dayNightSeed: 4242, dayNightConfig: cycleConfig);
        var capturedState = CreateState(level: 7, score: 321, health: 88);
        var capturedConfig = CreateConfig(autoSave: true);
        savingManager.SetState(capturedState, capturedConfig);

        savingManager.UpdateDayNightRuntime(10);
        savingManager.UpdateDayNightRuntime(3);

        var runtimeAtSave = GetRuntime(savingManager);
        var dayAtSave = savingManager.CurrentDayNightDay;
        var phaseAtSave = savingManager.CurrentDayNightPhase;
        var tickAtSave = runtimeAtSave.Tick;
        var waveTimerAtSave = runtimeAtSave.PhaseElapsedSeconds;

        var saveId = await savingManager.SaveGameAsync("slot-roundtrip");

        var loadingManager = new GameStateManager(store, options, dayNightSeed: 9999, dayNightConfig: cycleConfig);
        loadingManager.SetState(CreateState(level: 1, score: 1, health: 1), CreateConfig(autoSave: false));

        var loaded = await loadingManager.LoadGameAsync(saveId);

        loaded.state.Should().BeEquivalentTo(capturedState);
        loaded.config.Should().BeEquivalentTo(capturedConfig);

        var loadedRuntime = GetRuntime(loadingManager);
        loadingManager.CurrentDayNightDay.Should().Be(dayAtSave);
        loadingManager.CurrentDayNightPhase.Should().Be(phaseAtSave);
        loadedRuntime.Tick.Should().Be(tickAtSave);
        loadedRuntime.PhaseElapsedSeconds.Should().Be(waveTimerAtSave);

        int? expectedNextCheckpointToken = null;
        int? actualNextCheckpointToken = null;
        savingManager.OnDayNightCheckpoint += checkpoint => expectedNextCheckpointToken = checkpoint.RandomToken;
        loadingManager.OnDayNightCheckpoint += checkpoint => actualNextCheckpointToken = checkpoint.RandomToken;

        savingManager.UpdateDayNightRuntime(7);
        loadingManager.UpdateDayNightRuntime(7);

        actualNextCheckpointToken.Should().Be(expectedNextCheckpointToken);
    }

    // ACC:T25.14
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldRefuseLoad_WhenAutosaveFormatDiffersFromSelectedSerialization(bool enableCompression)
    {
        var store = new InMemoryDataStore();
        var options = new GameStateManagerOptions(StorageKey: "task25-format-autosave", EnableCompression: enableCompression);
        var manager = new GameStateManager(store, options);

        manager.SetState(CreateState(level: 3, score: 70, health: 66), CreateConfig(autoSave: true));
        var saveId = await manager.SaveGameAsync("slot-format");
        var originalPayload = await store.LoadAsync(saveId);
        originalPayload.Should().NotBeNull();

        var mismatchedPayload = enableCompression
            ? ConvertCompressedPayloadToJson(originalPayload!)
            : ConvertJsonPayloadToCompressed(originalPayload!);

        await store.SaveAsync(saveId, mismatchedPayload);

        Func<Task> act = () => manager.LoadGameAsync(saveId);
        var exceptionAssertions = await act.Should().ThrowAsync<InvalidOperationException>();
        exceptionAssertions.Which.Message.Should().Contain("format");
    }

    // ACC:T25.6
    [Fact]
    public async Task ShouldReportExplicitFailureAndKeepRuntimeStateUnchanged_WhenDeserializationFailsForSelectedFormat()
    {
        var store = new InMemoryDataStore();
        var options = new GameStateManagerOptions(StorageKey: "task25-failure-autosave", EnableCompression: false);
        var cycleConfig = new DayNightCycleConfig(DayDurationSeconds: 10, NightDurationSeconds: 10, MaxDay: 15);
        var manager = new GameStateManager(store, options, dayNightSeed: 123, dayNightConfig: cycleConfig);

        var baselineState = CreateState(level: 9, score: 900, health: 77);
        var baselineConfig = CreateConfig(autoSave: true);
        manager.SetState(baselineState, baselineConfig);
        manager.UpdateDayNightRuntime(4);

        var baselineRuntime = GetRuntime(manager);
        var baselineDay = manager.CurrentDayNightDay;
        var baselinePhase = manager.CurrentDayNightPhase;
        var baselineTick = baselineRuntime.Tick;
        var baselineWaveTimer = baselineRuntime.PhaseElapsedSeconds;

        const string brokenSaveId = "task25-failure-autosave-corrupted";
        await store.SaveAsync(brokenSaveId, "{ invalid json payload");

        Func<Task> act = () => manager.LoadGameAsync(brokenSaveId);
        var exceptionAssertions = await act.Should().ThrowAsync<InvalidOperationException>();
        exceptionAssertions.Which.Message.Should().Contain("deserialize");

        manager.GetState().Should().BeEquivalentTo(baselineState);
        manager.GetConfig().Should().BeEquivalentTo(baselineConfig);

        var runtimeAfterFailure = GetRuntime(manager);
        manager.CurrentDayNightDay.Should().Be(baselineDay);
        manager.CurrentDayNightPhase.Should().Be(baselinePhase);
        runtimeAfterFailure.Tick.Should().Be(baselineTick);
        runtimeAfterFailure.PhaseElapsedSeconds.Should().Be(baselineWaveTimer);
    }

    private static GameState CreateState(int level, int score, int health)
    {
        return new GameState(
            Id: $"state-{level}-{score}-{health}",
            Level: level,
            Score: score,
            Health: health,
            Inventory: new[] { "wood", "stone", "gold" },
            Position: new Position(12.5, -3.75),
            Timestamp: new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
    }

    private static GameConfig CreateConfig(bool autoSave)
    {
        return new GameConfig(
            MaxLevel: 99,
            InitialHealth: 120,
            ScoreMultiplier: 1.5,
            AutoSave: autoSave,
            Difficulty: Difficulty.Hard);
    }

    private static DayNightRuntimeStateMachine GetRuntime(GameStateManager manager)
    {
        var runtimeField = typeof(GameStateManager).GetField("_dayNightRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
        runtimeField.Should().NotBeNull();

        var runtime = runtimeField!.GetValue(manager);
        runtime.Should().BeOfType<DayNightRuntimeStateMachine>();
        return (DayNightRuntimeStateMachine)runtime!;
    }

    private static string ConvertJsonPayloadToCompressed(string jsonPayload)
    {
        jsonPayload.StartsWith("gz:", StringComparison.Ordinal).Should().BeFalse();
        var bytes = Encoding.UTF8.GetBytes(jsonPayload);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return "gz:" + Convert.ToBase64String(output.ToArray());
    }

    private static string ConvertCompressedPayloadToJson(string compressedPayload)
    {
        compressedPayload.StartsWith("gz:", StringComparison.Ordinal).Should().BeTrue();

        var bytes = Convert.FromBase64String(compressedPayload.Substring(3));
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
