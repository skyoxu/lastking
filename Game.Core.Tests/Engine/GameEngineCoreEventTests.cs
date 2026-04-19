using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Engine;
using Game.Core.Ports;
using Game.Core.Services;
using Game.Core.State;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Engine;

public class GameEngineCoreEventTests
{
    private sealed class CapturingEventBus : IEventBus
    {
        public List<DomainEvent> Published { get; } = new();

        public Task PublishAsync(DomainEvent evt)
        {
            Published.Add(evt);
            return Task.CompletedTask;
        }

        public IDisposable Subscribe(Func<DomainEvent, Task> handler) => new DummySubscription();

        private sealed class DummySubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static GameEngineCore CreateEngineAndBus(out CapturingEventBus bus)
    {
        var config = new GameConfig(
            MaxLevel: 10,
            InitialHealth: 100,
            ScoreMultiplier: 1.0,
            AutoSave: false,
            Difficulty: Difficulty.Medium
        );
        var inventory = new Inventory();
        bus = new CapturingEventBus();
        return new GameEngineCore(config, inventory, bus);
    }

    private sealed class InMemoryDataStore : IDataStore
    {
        public Task SaveAsync(string key, string json) => Task.CompletedTask;
        public Task<string?> LoadAsync(string key) => Task.FromResult<string?>(null);
        public Task DeleteAsync(string key) => Task.CompletedTask;
    }

    private static ChannelBudgetConfiguration CreateTask10ChannelConfig() =>
        new(
            Normal: new ChannelRule(Day1Budget: 50, DailyGrowth: 1.2m, ChannelLimit: 20, CostPerEnemy: 10),
            Elite: new ChannelRule(Day1Budget: 120, DailyGrowth: 1.2m, ChannelLimit: 8, CostPerEnemy: 20),
            Boss: new ChannelRule(Day1Budget: 300, DailyGrowth: 1.2m, ChannelLimit: 3, CostPerEnemy: 100));

    private static GameState CreateInitialState(int health) =>
        new(
            Id: Guid.NewGuid().ToString("N"),
            Level: 1,
            Score: 0,
            Health: health,
            Inventory: Array.Empty<string>(),
            Position: new Position(0, 0),
            Timestamp: DateTime.UtcNow);

    // ACC:T10.1
    // ACC:T10.3
    // ACC:T16.3
    // ACC:T32.3
    [Fact]
    public void ShouldPublishGameStartedEvent_WhenStartCalled()
    {
        // Arrange
        var engine = CreateEngineAndBus(out var bus);

        // Act
        engine.Start();

        // Assert
        bus.Published.Should().ContainSingle();
        var evt = bus.Published[0];
        evt.Type.Should().Be("game.started");
        evt.Source.Should().Be(nameof(GameEngineCore));
        evt.DataElement.HasValue.Should().BeTrue();
    }

    // ACC:T10.1
    // ACC:T10.4
    [Fact]
    public void ShouldVerifyRuntimeSpawnFallbackAndTerminalInSingleExecution_WhenRunningTask10IntegrationGate()
    {
        var trace = new List<string>();

        var engine = CreateEngineAndBus(out var bus);
        engine.Start();
        bus.Published.Should().ContainSingle(evt => evt.Type == "game.started");
        trace.Add("runtime-loop");

        var waveManager = new WaveManager();
        var wave = waveManager.Generate(
            dayIndex: 1,
            channelBudgetConfiguration: CreateTask10ChannelConfig(),
            seed: 20260405);
        wave.ChannelResults.Keys.Should().BeEquivalentTo(new[] { "normal", "elite", "boss" });
        trace.Add("spawn-channels");

        var selector = new EnemyAiTargetSelector();
        var blockedDecision = selector.SelectTarget(new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("wall-1", 2),
        });
        blockedDecision.IsFallbackAttack.Should().BeTrue();
        blockedDecision.TargetId.Should().Be("wall-1");
        trace.Add("path-fallback");

        var recoveredDecision = selector.SelectTarget(new[]
        {
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("wall-1", 2),
        });
        recoveredDecision.IsFallbackAttack.Should().BeFalse();
        recoveredDecision.TargetId.Should().Be("unit-1");

        var manager = new GameStateManager(
            store: new InMemoryDataStore(),
            dayNightSeed: 10,
            dayNightConfig: new DayNightCycleConfig(DayDurationSeconds: 1, NightDurationSeconds: 1, MaxDay: 15));
        manager.SetState(CreateInitialState(health: 30), config: null);

        var terminalEvents = 0;
        manager.OnRunTerminal += _ => terminalEvents += 1;
        for (var i = 0; i < 128 && !manager.IsRunTerminal; i++)
        {
            manager.UpdateDayNightRuntime(2);
        }

        manager.IsRunTerminal.Should().BeTrue();
        terminalEvents.Should().Be(1);
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
        trace.Add("terminal-outcome");

        trace.Should().ContainInOrder("runtime-loop", "spawn-channels", "path-fallback", "terminal-outcome");
    }

    // ACC:T15.3
    [Fact]
    public void ShouldKeepDeterministicReplayStableWithoutMutatingCoreEventContract_WhenInputsAreFixed()
    {
        var plan = new[]
        {
            new BuildingOperationPlan("barracks-1", "upgrade", 2),
            new BuildingOperationPlan("wall-1", "repair", 2),
        };
        var steps = new[] { 1, 1 };

        var replayA = new BuildingOperationDeterminismReplay(plan).Replay(steps);
        var replayB = new BuildingOperationDeterminismReplay(plan).Replay(steps);

        replayA.TickTimeline.Should().Equal(replayB.TickTimeline);
        replayA.CompletedOperations.Should().Equal(replayB.CompletedOperations);

        var engine = CreateEngineAndBus(out var bus);
        engine.Start();
        engine.Move(1, 0);
        engine.AddScore(5);

        bus.Published.Select(e => e.Type).Should().ContainInOrder("game.started", "player.moved", "score.changed");
    }

    [Fact]
    public void ShouldPublishScoreChangedEvent_WhenAddScoreCalled()
    {
        // Arrange
        var engine = CreateEngineAndBus(out var bus);
        engine.Start();
        bus.Published.Clear();

        // Act
        engine.AddScore(10);

        // Assert
        bus.Published.Should().ContainSingle();
        var evt = bus.Published[0];
        evt.Type.Should().Be("score.changed");
        evt.Source.Should().Be(nameof(GameEngineCore));
        evt.DataElement.HasValue.Should().BeTrue();
    }

    [Fact]
    public void ShouldExposeTask29WindowsBaselineGate_WhenEngineCanStartAndEmitCoreEvent()
    {
        var engine = CreateEngineAndBus(out var bus);
        engine.Start();

        bus.Published.Should().ContainSingle(evt => evt.Type == "game.started");
    }

    [Fact]
    public void ShouldKeepTask29DeterministicStateMachinePathStable_WhenSequenceIsFixed()
    {
        var fsm = new GameStateMachine();

        fsm.Start().Should().BeTrue();
        fsm.Pause().Should().BeTrue();
        fsm.Resume().Should().BeTrue();
        fsm.End().Should().BeTrue();

        fsm.State.Should().Be(GameFlowState.GameOver);
    }

    [Fact]
    public void ShouldPublishPlayerHealthChangedEvent_WhenApplyDamageCalled()
    {
        // Arrange
        var engine = CreateEngineAndBus(out var bus);
        engine.Start();
        bus.Published.Clear();

        // Act
        engine.ApplyDamage(new Damage(Amount: 10, Type: DamageType.Physical, IsCritical: false));

        // Assert
        bus.Published.Should().ContainSingle();
        var evt = bus.Published[0];
        evt.Type.Should().Be("player.health.changed");
        evt.Source.Should().Be(nameof(GameEngineCore));
        evt.DataElement.HasValue.Should().BeTrue();
    }

    [Fact]
    public void ShouldUpdateStateAndPublishPlayerMovedEvent_WhenMoveCalled()
    {
        var engine = CreateEngineAndBus(out var bus);
        engine.Start();
        bus.Published.Clear();

        var state = engine.Move(3, 4);

        state.Position.X.Should().Be(3);
        state.Position.Y.Should().Be(4);
        bus.Published.Should().ContainSingle();
        bus.Published[0].Type.Should().Be("player.moved");
    }

    [Fact]
    public void ShouldPublishGameEndedEventAndReturnResult_WhenEndCalled()
    {
        var engine = CreateEngineAndBus(out var bus);
        engine.Start();
        engine.Move(1, 2);
        engine.AddScore(5);
        bus.Published.Clear();

        var result = engine.End();

        result.FinalScore.Should().BeGreaterThanOrEqualTo(5);
        result.Statistics.TotalMoves.Should().Be(1);
        bus.Published.Should().ContainSingle();
        bus.Published[0].Type.Should().Be("game.ended");
    }

    // ACC:T12.2
    [Fact]
    [Trait("acceptance", "ACC:T12.2")]
    public void ShouldShareSameResourceEventContractAcrossCorePublishers_WhenResourceMutates()
    {
        var bus = new CapturingEventBus();
        var manager = new ResourceManager(bus, runId: "run-12", dayNumber: 1);

        manager.TryAdd(20, 0, 0, "contract-check").Succeeded.Should().BeTrue();

        var contractCheckEvents = bus.Published
            .Where(evt =>
                evt.Type == EventTypes.LastkingResourcesChanged
                && evt.DataElement.HasValue
                && evt.DataElement.Value.TryGetProperty("reason", out var reason)
                && string.Equals(reason.GetString(), "contract-check", StringComparison.Ordinal))
            .ToList();

        contractCheckEvents.Should().ContainSingle();
        contractCheckEvents[0].Type.Should().Be(EventTypes.LastkingResourcesChanged);
        contractCheckEvents[0].DataElement.HasValue.Should().BeTrue();
        var payload = contractCheckEvents[0].DataElement!.Value;
        payload.GetProperty("gold").GetInt32().Should().Be(820);
        payload.GetProperty("iron").GetInt32().Should().Be(150);
        payload.GetProperty("populationCap").GetInt32().Should().Be(50);
        payload.GetProperty("runId").GetString().Should().Be("run-12");
        payload.GetProperty("dayNumber").GetInt32().Should().Be(1);
        payload.GetProperty("reason").GetString().Should().Be("contract-check");
        payload.GetProperty("delta").GetProperty("gold").GetInt32().Should().Be(20);
        payload.GetProperty("delta").GetProperty("iron").GetInt32().Should().Be(0);
        payload.GetProperty("delta").GetProperty("populationCap").GetInt32().Should().Be(0);
    }
}
