using FluentAssertions;
using Game.Core.Services;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BattleInitializationFromConfigTests
{
    // ACC:T7.4
    [Fact]
    public void ShouldInitializeCastleHpFromConfiguredStartHp_WhenBattleStarts()
    {
        var config = LoadConfigWithCastleStartHp(160);
        var flow = new GameStateMachine();

        var runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(config.Snapshot.CastleStartHp),
            flow,
            "run-7",
            1);

        config.Accepted.Should().BeTrue();
        runtime.CurrentHp.Should().Be(160);
        runtime.IsAlive.Should().BeTrue();
        flow.State.Should().Be(GameFlowState.Running);
    }

    // ACC:T7.4
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldRejectNonPositiveConfiguredStartHp_WhenBattleStarts(int invalidStartHp)
    {
        var result = LoadConfigWithCastleStartHp(invalidStartHp);

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain(ConfigManager.OutOfRangeReason);
    }

    // ACC:T7.14
    [Fact]
    public void ShouldChangeCastleHpAtBattleStart_WhenConfiguredStartHpChanges()
    {
        var firstConfig = LoadConfigWithCastleStartHp(80);
        var secondConfig = LoadConfigWithCastleStartHp(125);

        var firstRuntime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(firstConfig.Snapshot.CastleStartHp),
            new GameStateMachine(),
            "run-a",
            1);
        var secondRuntime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(secondConfig.Snapshot.CastleStartHp),
            new GameStateMachine(),
            "run-b",
            1);

        firstRuntime.CurrentHp.Should().Be(80);
        secondRuntime.CurrentHp.Should().Be(125);
    }

    private static ConfigLoadResult LoadConfigWithCastleStartHp(int castleStartHp)
    {
        var manager = new ConfigManager();
        var json = $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": 120 },
          "waves": { "normal": { "day1_budget": 10, "daily_growth": 1.0 } },
          "channels": { "elite": "elite_a", "boss": "boss_a" },
          "spawn": { "cadence_seconds": 10 },
          "boss": { "count": 1 },
          "battle": { "castle_start_hp": {{castleStartHp}} }
        }
        """;
        return manager.LoadInitialFromJson(json, "battle-config.json");
    }
}
