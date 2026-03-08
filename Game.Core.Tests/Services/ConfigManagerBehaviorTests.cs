using System;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigManagerBehaviorTests
{
    [Fact]
    public void ShouldReturnParseErrorAndDefaultFallback_WhenJsonIsMalformed()
    {
        var manager = new ConfigManager();

        var result = manager.LoadInitialFromJson("{ bad", "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.ReasonCodes.Should().Contain(ConfigManager.ParseErrorReason);
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }

    [Fact]
    public void ShouldReturnMissingKeyReason_WhenRequiredPathsAreAbsent()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } }
                            }
                            """;

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain(ConfigManager.MissingKeyReason);
    }

    [Fact]
    public void ShouldReturnInvalidTypeReason_WhenValueTypeDoesNotMatch()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": "240", "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain(ConfigManager.InvalidTypeReason);
    }

    [Fact]
    public void ShouldReturnOutOfRangeReason_WhenNumericValueIsBelowMinimum()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": -1.0 } },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain(ConfigManager.OutOfRangeReason);
    }

    [Fact]
    public void ShouldApplyOptionalDefaults_WhenSpawnAndBossKeysAreMissing()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Snapshot.SpawnCadenceSeconds.Should().Be(10);
        result.Snapshot.BossCount.Should().Be(2);
    }

    [Fact]
    public void ShouldUseOptionalDefaultsAndReturnValidationErrors_WhenOptionalFieldsAreInvalid()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" },
                              "spawn": { "cadence_seconds": "x" },
                              "boss": { "count": 0 }
                            }
                            """;

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.ReasonCodes.Should().Contain(ConfigManager.InvalidTypeReason);
        result.ReasonCodes.Should().Contain(ConfigManager.OutOfRangeReason);
    }

    [Fact]
    public void ShouldKeepPreviousSnapshot_WhenReloadPayloadIsInvalid()
    {
        var manager = new ConfigManager();
        const string validJson = """
                                 {
                                   "time": { "day_seconds": 240, "night_seconds": 120 },
                                   "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                                   "channels": { "elite": "elite", "boss": "boss" },
                                   "spawn": { "cadence_seconds": 10 },
                                   "boss": { "count": 2 }
                                 }
                                 """;

        var first = manager.LoadInitialFromJson(validJson, "res://Config/balance.json");
        var second = manager.ReloadFromJson("{ invalid", "res://Config/balance.json");

        second.Accepted.Should().BeFalse();
        second.Source.Should().Be("fallback");
        second.Snapshot.Should().Be(first.Snapshot);
    }

    [Fact]
    public void ShouldCreateConfigLoadedEvent_WithStableMetadata()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var loaded = manager.LoadInitialFromJson(json, "res://Config/balance.json");
        var evt = loaded.ToEvent(DateTimeOffset.Parse("2026-03-08T00:00:00+08:00"));

        evt.SourcePath.Should().Be("res://Config/balance.json");
        evt.ConfigHash.Should().Be(loaded.ConfigHash);
        evt.LoadedAt.Should().Be(DateTimeOffset.Parse("2026-03-08T00:00:00+08:00"));
    }

    [Fact]
    public void ShouldUseDefaultSnapshot_WhenReloadIsCalledBeforeInitialLoad()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var result = manager.ReloadFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.ReasonCodes.Should().Contain(ConfigManager.InvalidOrderReason);
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
    }
}
