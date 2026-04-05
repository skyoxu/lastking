using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Domain;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.State;

public class GameStateManagerTests
{
    private sealed class InMemoryDataStore : IDataStore
    {
        private readonly Dictionary<string, string> _dict = new();

        public Task SaveAsync(string key, string json)
        {
            _dict[key] = json;
            return Task.CompletedTask;
        }

        public Task<string?> LoadAsync(string key)
        {
            _dict.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task DeleteAsync(string key)
        {
            _dict.Remove(key);
            return Task.CompletedTask;
        }

        public IReadOnlyDictionary<string, string> Snapshot => _dict;
    }

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

    // ACC:T3.4
    // ACC:T3.7
    // ACC:T3.12
    [Fact]
    public void ShouldTransitionAtConfiguredThresholds_WhenAdvancingFromDay1()
    {
        var manager = CreateDayNightManager(seed: 10);

        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(1);

        manager.UpdateDayNightRuntime(239);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(1);

        manager.UpdateDayNightRuntime(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        manager.CurrentDayNightDay.Should().Be(1);

        manager.UpdateDayNightRuntime(120);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(2);
    }

    // ACC:T3.7
    [Fact]
    public void ShouldRejectManualTransitionBeforeThreshold_WhenRequestingPhaseChange()
    {
        var manager = CreateDayNightManager(seed: 101);
        var checkpointEvents = 0;
        manager.OnDayNightCheckpoint += _ => checkpointEvents += 1;

        manager.RequestDayNightTransition(DayNightPhase.Night).Should().BeFalse();
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.DayNightCheckpointCount.Should().Be(0);
        checkpointEvents.Should().Be(0);

        manager.UpdateDayNightRuntime(100);
        manager.RequestDayNightTransition(DayNightPhase.Night).Should().BeFalse();
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.DayNightCheckpointCount.Should().Be(0);
        checkpointEvents.Should().Be(0);

        manager.UpdateDayNightRuntime(140);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        manager.DayNightCheckpointCount.Should().Be(1);
        checkpointEvents.Should().Be(1);

        manager.UpdateDayNightRuntime(60);
        manager.RequestDayNightTransition(DayNightPhase.Day).Should().BeFalse();
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.DayNightCheckpointCount.Should().Be(1);
        checkpointEvents.Should().Be(1);
    }

    // ACC:T3.1
    // ACC:T3.2
    [Fact]
    public void ShouldSupportForcedTerminalWithSingleEmission_WhenUsingManagerRuntime()
    {
        var manager = CreateDayNightManager(seed: 33);
        var baseline = CreateDayNightManager(seed: 33);
        var terminalEvents = 0;
        var baselineTerminalEvents = 0;
        var checkpoints = 0;
        var baselineCheckpoints = 0;
        manager.OnDayNightTerminal += _ => terminalEvents += 1;
        manager.OnDayNightCheckpoint += _ => checkpoints += 1;
        baseline.OnDayNightTerminal += _ => baselineTerminalEvents += 1;
        baseline.OnDayNightCheckpoint += _ => baselineCheckpoints += 1;

        manager.UpdateDayNightRuntime(240);
        baseline.UpdateDayNightRuntime(240);
        checkpoints.Should().Be(1);
        baselineCheckpoints.Should().Be(1);

        manager.ForceDayNightTerminal().Should().BeTrue();
        baseline.ForceDayNightTerminal().Should().BeTrue();
        manager.ForceDayNightTerminal().Should().BeFalse();
        baseline.ForceDayNightTerminal().Should().BeFalse();

        var dayAtTerminal = manager.CurrentDayNightDay;
        manager.UpdateDayNightRuntime(240);
        baseline.UpdateDayNightRuntime(240);
        manager.CurrentDayNightDay.Should().Be(dayAtTerminal);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Terminal);
        baseline.CurrentDayNightPhase.Should().Be(DayNightPhase.Terminal);
        baseline.CurrentDayNightDay.Should().Be(dayAtTerminal);
        terminalEvents.Should().Be(1);
        baselineTerminalEvents.Should().Be(1);
        checkpoints.Should().Be(baselineCheckpoints);
        checkpoints.Should().BeGreaterThan(0);
    }

    // ACC:T3.8
    // ACC:T3.10
    // ACC:T3.14
    // ACC:T10.10
    [Fact]
    public void ShouldKeepDayWithinRangeAndEmitTerminalOnce_WhenDay15Completes()
    {
        var manager = CreateDayNightManager(seed: 11);
        var terminalEvents = 0;
        manager.OnDayNightTerminal += _ => terminalEvents += 1;

        for (var i = 0; i < 2000; i++)
        {
            manager.UpdateDayNightRuntime(60);
            if (manager.CurrentDayNightPhase == DayNightPhase.Terminal)
            {
                break;
            }
        }

        manager.CurrentDayNightDay.Should().Be(15);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Terminal);
        terminalEvents.Should().Be(1);

        manager.UpdateDayNightRuntime(3600);
        manager.CurrentDayNightDay.Should().Be(15);
        terminalEvents.Should().Be(1);
    }

    // ACC:T3.5
    // ACC:T3.9
    // ACC:T3.18
    // ACC:T3.19
    // ACC:T10.5
    [Fact]
    public void ShouldReplayDeterministicallyWithCheckpointParity_WhenSeedAndInputMatch()
    {
        var first = CreateDayNightManager(seed: 99);
        var second = CreateDayNightManager(seed: 99);

        var firstCheckpoints = new List<DayNightCheckpoint>();
        var secondCheckpoints = new List<DayNightCheckpoint>();
        first.OnDayNightCheckpoint += firstCheckpoints.Add;
        second.OnDayNightCheckpoint += secondCheckpoints.Add;

        var updates = Enumerable.Repeat(10d, 60).ToArray();
        var firstObservedBoundaries = 0;
        var secondObservedBoundaries = 0;
        var firstPhase = first.CurrentDayNightPhase;
        var secondPhase = second.CurrentDayNightPhase;
        foreach (var delta in updates)
        {
            first.UpdateDayNightRuntime(delta);
            second.UpdateDayNightRuntime(delta);
            if (first.CurrentDayNightPhase != firstPhase)
            {
                firstObservedBoundaries += 1;
                firstPhase = first.CurrentDayNightPhase;
            }

            if (second.CurrentDayNightPhase != secondPhase)
            {
                secondObservedBoundaries += 1;
                secondPhase = second.CurrentDayNightPhase;
            }
        }

        first.CurrentDayNightDay.Should().Be(second.CurrentDayNightDay);
        first.CurrentDayNightPhase.Should().Be(second.CurrentDayNightPhase);
        firstCheckpoints.Count.Should().Be(secondCheckpoints.Count);
        first.DayNightCheckpointCount.Should().Be(firstCheckpoints.Count);
        second.DayNightCheckpointCount.Should().Be(secondCheckpoints.Count);
        firstCheckpoints.Count.Should().Be(firstObservedBoundaries);
        secondCheckpoints.Count.Should().Be(secondObservedBoundaries);
        firstCheckpoints.Select(x => x.RandomToken).Should().Equal(secondCheckpoints.Select(x => x.RandomToken));
    }

    // ACC:T3.11
    // ACC:T3.13
    // ACC:T3.15
    [Fact]
    public void ShouldAdvanceMonotonicallyOnlyOnActiveUpdates_WhenUsingFixedAndVariableSteps()
    {
        var fixedStep = CreateDayNightManager(seed: 13);
        var variableStep = CreateDayNightManager(seed: 13);

        fixedStep.UpdateDayNightRuntime(120, isActiveUpdate: false);
        fixedStep.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        fixedStep.CurrentDayNightDay.Should().Be(1);

        for (var i = 0; i < 24; i++)
        {
            fixedStep.UpdateDayNightRuntime(10);
        }

        var variableInputs = new[] { 33d, 17d, 50d, 40d, 100d };
        foreach (var delta in variableInputs)
        {
            variableStep.UpdateDayNightRuntime(delta);
        }

        fixedStep.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        variableStep.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        fixedStep.CurrentDayNightDay.Should().Be(1);
        variableStep.CurrentDayNightDay.Should().Be(1);
    }

    // ACC:T3.16
    [Fact]
    public void ShouldExposeObservableDayNightStateAndSignals_WhenRuntimeUpdates()
    {
        var manager = CreateDayNightManager(seed: 21);
        var checkpoints = 0;
        var terminals = 0;
        manager.OnDayNightCheckpoint += _ => checkpoints += 1;
        manager.OnDayNightTerminal += _ => terminals += 1;

        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.CurrentDayNightDay.Should().Be(1);

        manager.UpdateDayNightRuntime(240);
        checkpoints.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        manager.CurrentDayNightDay.Should().Be(1);

        manager.ForceDayNightTerminal().Should().BeTrue();
        terminals.Should().Be(1);
    }

    // ACC:T8.1
    // ACC:T8.4
    // ACC:T10.14
    [Fact]
    public void ShouldEnterWinAndRaiseVisibleFeedback_WhenDay15ReachedWithCastleHpAboveZero()
    {
        var manager = CreateWinConditionManager(seed: 71, castleHp: 10, endOfGameHandling: EndOfGameHandling.Pause);
        var events = new List<DomainEvent>();
        var runTerminalEvents = 0;
        manager.OnEvent(events.Add);
        manager.OnRunTerminal += _ => runTerminalEvents += 1;

        AdvanceToDay(manager, targetDay: 15);

        manager.CurrentDayNightDay.Should().Be(15);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
        manager.IsWinPresentationVisible.Should().BeTrue();
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Terminal);
        runTerminalEvents.Should().Be(1);

        var uiFeedbackEvent = events.Single(x => x.Type == EventTypes.LastkingUiFeedbackRaised);
        using var uiFeedbackJson = JsonDocument.Parse(uiFeedbackEvent.DataJson);
        uiFeedbackJson.RootElement.GetProperty("Code").GetString().Should().Be("run.win.day15");
        uiFeedbackJson.RootElement.GetProperty("MessageKey").GetString().Should().Be("ui.run.win.day15");
    }

    // ACC:T8.2
    // ACC:T8.5
    // ACC:T10.14
    [Fact]
    public void ShouldPreferLossAndBlockWin_WhenCastleHpBecomesZeroBeforeDay15()
    {
        var manager = CreateWinConditionManager(seed: 72, castleHp: 12);
        var events = new List<DomainEvent>();
        manager.OnEvent(events.Add);

        manager.UpdateDayNightRuntime(2);
        manager.UpdateDayNightRuntime(2);
        manager.ApplyCastleDamage(12);

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);

        for (var i = 0; i < 20; i++)
        {
            manager.UpdateDayNightRuntime(2);
        }

        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
        events.Count(x => x.Type == EventTypes.LastkingUiFeedbackRaised).Should().Be(0);
    }

    // ACC:T8.3
    // ACC:T8.6
    [Fact]
    public void ShouldResolveDeterministicallyForTerminalOutcome_WhenSeedAndInputsMatch()
    {
        var first = CreateWinConditionManager(seed: 73, castleHp: 20);
        var second = CreateWinConditionManager(seed: 73, castleHp: 20);

        for (var i = 0; i < 6; i++)
        {
            first.UpdateDayNightRuntime(2);
            second.UpdateDayNightRuntime(2);
        }

        first.ApplyCastleDamage(4);
        second.ApplyCastleDamage(4);
        AdvanceToDay(first, targetDay: 15);
        AdvanceToDay(second, targetDay: 15);

        first.CurrentRunTerminalOutcome.Should().Be(second.CurrentRunTerminalOutcome);
        first.CurrentDayNightDay.Should().Be(second.CurrentDayNightDay);
        first.IsWinPresentationVisible.Should().Be(second.IsWinPresentationVisible);
    }

    // ACC:T8.7
    // ACC:T8.15
    [Fact]
    public void ShouldRequireExplicitRestartBeforeProgressionResumes_WhenRunAlreadyTerminal()
    {
        var manager = CreateWinConditionManager(seed: 74, castleHp: 15, endOfGameHandling: EndOfGameHandling.Reset);
        AdvanceToDay(manager, targetDay: 15);

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
        manager.LastRunTerminalState.Should().NotBeNull();
        manager.LastRunTerminalState!.Day.Should().Be(15);
        manager.LastRunTerminalState!.AppliedHandling.Should().Be(EndOfGameHandling.Reset);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);

        manager.UpdateDayNightRuntime(2);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);

        manager.RestartRun(startingCastleHp: 15).Should().BeTrue();
        manager.IsRunTerminal.Should().BeFalse();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.None);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);

        manager.UpdateDayNightRuntime(2);
        manager.CurrentDayNightDay.Should().Be(2);
    }

    // ACC:T8.7
    // ACC:T8.15
    [Fact]
    public void ShouldFreezeRuntimeAfterTerminal_WhenEndOfGameHandlingIsPause()
    {
        var manager = CreateWinConditionManager(seed: 741, castleHp: 5, endOfGameHandling: EndOfGameHandling.Pause);

        manager.ApplyCastleDamage(5);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
        manager.LastRunTerminalState.Should().NotBeNull();
        manager.LastRunTerminalState!.AppliedHandling.Should().Be(EndOfGameHandling.Pause);

        var dayAtTerminal = manager.CurrentDayNightDay;
        var phaseAtTerminal = manager.CurrentDayNightPhase;
        var tickAtTerminal = manager.LastRunTerminalState!.Tick;

        manager.UpdateDayNightRuntime(2);
        manager.UpdateDayNightRuntime(60);
        manager.UpdateDayNightRuntime(600);

        manager.CurrentDayNightDay.Should().Be(dayAtTerminal);
        manager.CurrentDayNightPhase.Should().Be(phaseAtTerminal);
        manager.LastRunTerminalState!.Tick.Should().Be(tickAtTerminal);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
    }

    // ACC:T8.8
    // ACC:T8.9
    [Fact]
    public void ShouldStayNonTerminalBeforeDay15AndWinOnFirstDay15Step_WhenCastleIsIntact()
    {
        var manager = CreateWinConditionManager(seed: 75, castleHp: 18);

        AdvanceToDay(manager, targetDay: 14);
        manager.IsRunTerminal.Should().BeFalse();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.None);

        manager.UpdateDayNightRuntime(2);
        manager.CurrentDayNightDay.Should().Be(15);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
    }

    // ACC:T8.11
    [Fact]
    public void ShouldNotEmitDuplicateTerminalOrWinFeedback_WhenTickingAfterWin()
    {
        var manager = CreateWinConditionManager(seed: 76, castleHp: 18);
        var terminalEvents = 0;
        var uiFeedbackEvents = 0;
        manager.OnRunTerminal += _ => terminalEvents += 1;
        manager.OnEvent(x =>
        {
            if (x.Type == EventTypes.LastkingUiFeedbackRaised)
            {
                uiFeedbackEvents += 1;
            }
        });

        AdvanceToDay(manager, targetDay: 15);
        manager.UpdateDayNightRuntime(100);
        manager.UpdateDayNightRuntime(100);

        terminalEvents.Should().Be(1);
        uiFeedbackEvents.Should().Be(1);
    }

    // ACC:T8.11
    [Fact]
    public void ShouldNotEmitDuplicateTerminalOrWinFeedback_WhenTickingAfterLoss()
    {
        var manager = CreateWinConditionManager(seed: 79, castleHp: 6);
        var terminalEvents = 0;
        var uiFeedbackEvents = 0;
        manager.OnRunTerminal += _ => terminalEvents += 1;
        manager.OnEvent(x =>
        {
            if (x.Type == EventTypes.LastkingUiFeedbackRaised)
            {
                uiFeedbackEvents += 1;
            }
        });

        manager.ApplyCastleDamage(6);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);

        manager.UpdateDayNightRuntime(100);
        manager.UpdateDayNightRuntime(100);

        terminalEvents.Should().Be(1);
        uiFeedbackEvents.Should().Be(0);
    }

    // ACC:T8.12
    // ACC:T8.16
    [Fact]
    public void ShouldFinishRunAsWinWithoutLoss_WhenPlaythroughKeepsCastleHpPositive()
    {
        var manager = CreateWinConditionManager(seed: 77, castleHp: 25);
        var outcomes = new List<RunTerminalOutcome>();
        manager.OnRunTerminal += state => outcomes.Add(state.Outcome);

        AdvanceToDay(manager, targetDay: 15);

        outcomes.Should().ContainSingle();
        outcomes[0].Should().Be(RunTerminalOutcome.Win);
        manager.LastRunTerminalState.Should().NotBeNull();
        manager.LastRunTerminalState!.CastleHp.Should().BeGreaterThan(0);
    }

    // ACC:T8.13
    // ACC:T8.14
    [Fact]
    public void ShouldReevaluateDayAndCastleHpOnEachUpdateAndLockLoss_WhenCastleHpDropsToZero()
    {
        var manager = CreateWinConditionManager(seed: 78, castleHp: 5);

        manager.UpdateDayNightRuntime(2);
        manager.UpdateDayNightRuntime(2);
        manager.SetCastleHp(0);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);

        manager.UpdateDayNightRuntime(100);
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
        manager.IsWinPresentationVisible.Should().BeFalse();
    }

    // ACC:T8.10
    [Fact]
    public void ShouldKeepLossAtDay15BoundaryUntilRestart_WhenCastleHpIsZeroOnEvaluationStep()
    {
        var manager = CreateWinConditionManager(seed: 80, castleHp: 6);
        AdvanceToDay(manager, targetDay: 14);
        manager.CurrentDayNightDay.Should().Be(14);

        manager.SetCastleHp(0);
        manager.UpdateDayNightRuntime(2);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
        manager.IsWinPresentationVisible.Should().BeFalse();

        manager.RestartRun(startingCastleHp: 6).Should().BeTrue();
        AdvanceToDay(manager, targetDay: 15);
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
    }

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

    private static GameStateManager CreateDayNightManager(int seed)
    {
        return new GameStateManager(
            store: new InMemoryDataStore(),
            dayNightSeed: seed,
            dayNightConfig: new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 15));
    }

    private static GameStateManager CreateWinConditionManager(int seed, int castleHp, EndOfGameHandling endOfGameHandling = EndOfGameHandling.Pause)
    {
        var manager = new GameStateManager(
            store: new InMemoryDataStore(),
            options: new GameStateManagerOptions(EndOfGameHandling: endOfGameHandling),
            dayNightSeed: seed,
            dayNightConfig: new DayNightCycleConfig(DayDurationSeconds: 1, NightDurationSeconds: 1, MaxDay: 15));

        manager.SetState(MakeState(level: 1, score: 0) with { Health = castleHp }, MakeConfig());
        return manager;
    }

    private static void AdvanceToDay(GameStateManager manager, int targetDay)
    {
        var guard = 0;
        while (!manager.IsRunTerminal && manager.CurrentDayNightDay < targetDay && guard < 100)
        {
            manager.UpdateDayNightRuntime(2);
            guard += 1;
        }

        guard.Should().BeLessThan(100);
    }
}

