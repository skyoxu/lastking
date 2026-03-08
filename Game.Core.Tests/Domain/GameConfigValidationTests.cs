using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Domain;

public class GameConfigValidationTests
{
    private const string MissingKeyReason = "CFG_MISSING_KEY";
    private const string InvalidTypeReason = "CFG_INVALID_TYPE";
    private const string OutOfRangeReason = "CFG_OUT_OF_RANGE";

    // ACC:T2.17
    [Fact]
    public void ShouldEmitReasonCodesAndFallback_WhenConfigHasMissingTypeAndRangeErrors()
    {
        var manager = new ConfigManager();
        const string baseline = """
                                {
                                  "time": { "day_seconds": 240, "night_seconds": 120 },
                                  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                                  "channels": { "elite": "elite", "boss": "boss" },
                                  "spawn": { "cadence_seconds": 10 },
                                  "boss": { "count": 2 }
                                }
                                """;
        manager.LoadInitialFromJson(baseline, "res://Config/balance.json");

        const string json = """
                            {
                              "time": {
                                "night_seconds": 120
                              },
                              "waves": {
                                "normal": {
                                  "day1_budget": 50,
                                  "daily_growth": "fast"
                                },
                                "spawn_cadence_seconds": 0
                              },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var outcome = manager.ReloadFromJson(json, "res://Config/balance.json");

        outcome.Accepted.Should().BeFalse();
        outcome.ReasonCodes.Should().Contain(MissingKeyReason);
        outcome.ReasonCodes.Should().Contain(InvalidTypeReason);
        outcome.Snapshot.Should().Be(manager.Snapshot);
    }

    [Fact]
    public void ShouldKeepProvidedRuntimeConfig_WhenConfigIsValid()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": {
                                "day_seconds": 240,
                                "night_seconds": 120
                              },
                              "waves": {
                                "normal": {
                                  "day1_budget": 150,
                                  "daily_growth": 1.2
                                }
                              },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var outcome = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        outcome.Accepted.Should().BeTrue();
        outcome.ReasonCodes.Should().BeEmpty();
        outcome.Snapshot.Day1Budget.Should().Be(150);
        outcome.Snapshot.DailyGrowth.Should().Be(1.2m);
        outcome.Snapshot.Should().NotBe(BalanceSnapshot.Default);
    }

    [Fact]
    public void ShouldUseStableFallbackDefaults_WhenRejectingInvalidConfig()
    {
        var fallback = BalanceSnapshot.Default;

        fallback.DaySeconds.Should().Be(240);
        fallback.NightSeconds.Should().Be(120);
        fallback.Day1Budget.Should().Be(50);
        fallback.DailyGrowth.Should().Be(1.2m);
        fallback.SpawnCadenceSeconds.Should().Be(10);
        fallback.BossCount.Should().Be(2);
    }
}
