using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Core.Contracts;
using Game.Core.Domain;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.State;

internal sealed class InMemoryDataStore : IDataStore
{
    private readonly Dictionary<string,string> _dict = new();
    public Task SaveAsync(string key, string json) { _dict[key] = json; return Task.CompletedTask; }
    public Task<string?> LoadAsync(string key) { _dict.TryGetValue(key, out var v); return Task.FromResult(v); }
    public Task DeleteAsync(string key) { _dict.Remove(key); return Task.CompletedTask; }
    public IReadOnlyDictionary<string,string> Snapshot => _dict;
}

public class GameStateManagerTests
{
    private static GameState MakeState(int level=1, int score=0)
        => new(
            Id: Guid.NewGuid().ToString(),
            Level: level,
            Score: score,
            Health: 100,
            Inventory: Array.Empty<string>(),
            Position: new Game.Core.Domain.ValueObjects.Position(0,0),
            Timestamp: DateTime.UtcNow
        );

    private static GameConfig MakeConfig()
        => new(
            MaxLevel: 50,
            InitialHealth: 100,
            ScoreMultiplier: 1.0,
            AutoSave: false,
            Difficulty: Difficulty.Medium
        );

    [Fact]
    public async Task ShouldSupportSaveLoadDeleteAndIndex_WhenCompressionEnabled()
    {
        var store = new InMemoryDataStore();
        var opts = new GameStateManagerOptions(MaxSaves: 2, EnableCompression: true);
        var mgr = new GameStateManager(store, opts);

        var seen = new List<string>();
        mgr.OnEvent(e => seen.Add(e.Type));

        mgr.SetState(MakeState(level:2), MakeConfig());
        var id1 = await mgr.SaveGameAsync("slot1");
        Assert.Contains("game.save.created", seen);
        Assert.True(store.Snapshot.ContainsKey(id1));
        Assert.StartsWith("gz:", store.Snapshot[id1]);

        mgr.SetState(MakeState(level:3), MakeConfig());
        var id2 = await mgr.SaveGameAsync("slot2");
        var list = await mgr.GetSaveListAsync();
        Assert.True(list.Count >= 2);

        // Trigger cleanup by saving third; MaxSaves=2 => first gets deleted from store
        mgr.SetState(MakeState(level:4), MakeConfig());
        var id3 = await mgr.SaveGameAsync("slot3");

        var saveIndexKey = opts.StorageKey + ":index";
        var indexJson = await store.LoadAsync(saveIndexKey);
        Assert.NotNull(indexJson);
        var ids = JsonSerializer.Deserialize<List<string>>(indexJson!)!;
        Assert.Equal(2, ids.Count);
        Assert.DoesNotContain(id1, ids);

        // load latest
        var (state, cfg) = await mgr.LoadGameAsync(id3);
        Assert.Equal(4, state.Level);
        Assert.Equal(100, cfg.InitialHealth);

        // delete second
        await mgr.DeleteSaveAsync(id2);
        indexJson = await store.LoadAsync(saveIndexKey);
        ids = JsonSerializer.Deserialize<List<string>>(indexJson!)!;
        Assert.DoesNotContain(id2, ids);
    }

    [Fact]
    public async Task ShouldToggleAndTickAutosave_WhenConfigurationChanges()
    {
        var store = new InMemoryDataStore();
        var mgr = new GameStateManager(store);
        mgr.SetState(MakeState(level:5), MakeConfig());
        mgr.EnableAutoSave();
        await mgr.AutoSaveTickAsync();
        mgr.DisableAutoSave();
        var idx = await store.LoadAsync("guild-manager-game:index");
        Assert.NotNull(idx);
    }

    [Fact]
    public async Task ShouldThrow_WhenSavingWithoutStateOrWithTooLongTitle()
    {
        var store = new InMemoryDataStore();
        var mgr = new GameStateManager(store);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await mgr.SaveGameAsync());

        mgr.SetState(MakeState(), MakeConfig());
        var tooLong = new string('x', 101);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await mgr.SaveGameAsync(tooLong));
    }

    [Fact]
    public async Task ShouldThrow_WhenScreenshotPayloadIsTooLarge()
    {
        var store = new InMemoryDataStore();
        var mgr = new GameStateManager(store);
        mgr.SetState(MakeState(), MakeConfig());
        var tooLargeScreenshot = new string('a', 2_000_001);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await mgr.SaveGameAsync("slot", tooLargeScreenshot));
    }

    [Fact]
    public async Task ShouldThrow_WhenLoadingCorruptedSaveChecksum()
    {
        var store = new InMemoryDataStore();
        var mgr = new GameStateManager(store, new GameStateManagerOptions(EnableCompression: false));
        mgr.SetState(MakeState(level: 2), MakeConfig());
        var saveId = await mgr.SaveGameAsync("slot-corrupt");

        var raw = await store.LoadAsync(saveId);
        Assert.NotNull(raw);
        var save = JsonSerializer.Deserialize<SaveData>(raw!)!;
        var corrupted = save with { Metadata = save.Metadata with { Checksum = "BAD-CHECKSUM" } };
        await store.SaveAsync(saveId, JsonSerializer.Serialize(corrupted));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await mgr.LoadGameAsync(saveId));
    }

    [Fact]
    public async Task ShouldNoop_WhenAutosaveTickRunsWithoutStateOrWhenDisabled()
    {
        var store = new InMemoryDataStore();
        var mgr = new GameStateManager(store);

        await mgr.AutoSaveTickAsync();
        mgr.EnableAutoSave();
        mgr.DisableAutoSave();
        await mgr.AutoSaveTickAsync();

        var idx = await store.LoadAsync("guild-manager-game:index");
        Assert.Null(idx);
    }

    [Fact]
    public async Task ShouldIgnoreBrokenEntries_WhenLoadingSaveList()
    {
        var store = new InMemoryDataStore();
        var mgr = new GameStateManager(store, new GameStateManagerOptions(EnableCompression: false));
        mgr.SetState(MakeState(level: 3), MakeConfig());
        var goodSaveId = await mgr.SaveGameAsync("slot-good");
        await store.SaveAsync("bad-save", "{not-json");
        var indexKey = "guild-manager-game:index";
        await store.SaveAsync(indexKey, "[\"bad-save\",\"" + goodSaveId + "\"]");

        var saves = await mgr.GetSaveListAsync();

        Assert.Single(saves);
        Assert.Equal(goodSaveId, saves[0].Id);
    }
}

