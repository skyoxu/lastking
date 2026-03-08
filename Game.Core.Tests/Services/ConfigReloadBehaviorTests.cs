using System;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class ConfigReloadBehaviorTests
{
    // ACC:T2.14
    [Fact]
    public void ShouldChangeRuntimeOutcomes_WhenBalancingConfigIsReplacedAndReloaded()
    {
        var manager = new ConfigManager();
        const string initialJson = """
                                   {
                                     "time": { "day_seconds": 240, "night_seconds": 120 },
                                     "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                                     "channels": { "elite": "elite", "boss": "boss" },
                                     "spawn": { "cadence_seconds": 10 },
                                     "boss": { "count": 2 }
                                   }
                                   """;
        const string replacedJson = """
                                    {
                                      "time": { "day_seconds": 180, "night_seconds": 90 },
                                      "waves": { "normal": { "day1_budget": 75, "daily_growth": 1.3 } },
                                      "channels": { "elite": "elite", "boss": "boss" },
                                      "spawn": { "cadence_seconds": 8 },
                                      "boss": { "count": 2 }
                                    }
                                    """;

        var initial = manager.LoadInitialFromJson(initialJson, "res://Config/balance.json");
        var beforeReload = BalanceRuntimeEvaluator.Evaluate(initial.Snapshot, dayIndex: 5);
        var reloaded = manager.ReloadFromJson(replacedJson, "res://Config/balance.json");
        var afterReload = BalanceRuntimeEvaluator.Evaluate(reloaded.Snapshot, dayIndex: 5);

        beforeReload.DayNightCycleSeconds.Should().Be(360);
        afterReload.DayNightCycleSeconds.Should().Be(270);

        afterReload.WaveBudget.Should().NotBe(beforeReload.WaveBudget);
        afterReload.WaveBudget.Should().BeGreaterThan(beforeReload.WaveBudget);

        afterReload.SpawnCadenceSeconds.Should().NotBe(beforeReload.SpawnCadenceSeconds);
        afterReload.SpawnsPerMinute.Should().BeGreaterThan(beforeReload.SpawnsPerMinute);

        afterReload.BossCount.Should().Be(2);
    }

    [Fact]
    public void ShouldKeepRuntimeOutcomesStable_WhenReloadUsesEquivalentConfig()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" },
                              "spawn": { "cadence_seconds": 10 },
                              "boss": { "count": 2 }
                            }
                            """;

        var first = manager.LoadInitialFromJson(json, "res://Config/balance.json");
        var beforeReload = BalanceRuntimeEvaluator.Evaluate(first.Snapshot, dayIndex: 3);
        var second = manager.ReloadFromJson(json, "res://Config/balance.json");
        var afterReload = BalanceRuntimeEvaluator.Evaluate(second.Snapshot, dayIndex: 3);

        afterReload.Should().BeEquivalentTo(beforeReload);
    }
}
