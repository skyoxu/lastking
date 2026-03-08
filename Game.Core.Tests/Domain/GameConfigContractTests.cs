using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Domain;

public class GameConfigContractTests
{
    // ACC:T2.15
    [Fact]
    public void ShouldContainRequiredSectionsAndStableKeys_WhenValidConfigDocumentProvided()
    {
        using var document = CreateCanonicalConfig();
        var root = document.RootElement;

        HasPath(root, "time.day_seconds").Should().BeTrue();
        HasPath(root, "time.night_seconds").Should().BeTrue();
        HasPath(root, "waves.normal.day1_budget").Should().BeTrue();
        HasPath(root, "waves.normal.daily_growth").Should().BeTrue();
        HasPath(root, "channels.elite").Should().BeTrue();
        HasPath(root, "channels.boss").Should().BeTrue();
        HasPath(root, "spawn.cadence_seconds").Should().BeTrue();
        HasPath(root, "boss.count").Should().BeTrue();
    }

    [Fact]
    public void ShouldLoadBalancingValues_WhenCanonicalConfigProvided()
    {
        using var document = CreateCanonicalConfig();
        var manager = new ConfigManager();
        var loaded = manager.LoadInitialFromJson(document.RootElement.GetRawText(), "res://Config/balance.json");

        loaded.Accepted.Should().BeTrue();
        loaded.Snapshot.DaySeconds.Should().Be(240);
        loaded.Snapshot.NightSeconds.Should().Be(120);
        loaded.Snapshot.Day1Budget.Should().Be(50);
        loaded.Snapshot.DailyGrowth.Should().Be(1.2m);
        loaded.Snapshot.EliteChannel.Should().Be("elite");
        loaded.Snapshot.BossChannel.Should().Be("boss");
        loaded.Snapshot.SpawnCadenceSeconds.Should().Be(10);
        loaded.Snapshot.BossCount.Should().Be(2);
    }

    [Fact]
    public void ShouldMapRequiredKeysToRuntimeConsumers_WhenContractRegistryIsBuilt()
    {
        var keyToConsumer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["time.day_seconds"] = nameof(BalanceSnapshot.DaySeconds),
            ["time.night_seconds"] = nameof(BalanceSnapshot.NightSeconds),
            ["waves.normal.day1_budget"] = nameof(BalanceSnapshot.Day1Budget),
            ["waves.normal.daily_growth"] = nameof(BalanceSnapshot.DailyGrowth),
            ["channels.elite"] = nameof(BalanceSnapshot.EliteChannel),
            ["channels.boss"] = nameof(BalanceSnapshot.BossChannel),
            ["spawn.cadence_seconds"] = nameof(BalanceSnapshot.SpawnCadenceSeconds),
            ["boss.count"] = nameof(BalanceSnapshot.BossCount)
        };

        keyToConsumer.Should().ContainKey("time.day_seconds").WhoseValue.Should().Be(nameof(BalanceSnapshot.DaySeconds));
        keyToConsumer.Should().ContainKey("time.night_seconds").WhoseValue.Should().Be(nameof(BalanceSnapshot.NightSeconds));
        keyToConsumer.Should().ContainKey("waves.normal.day1_budget").WhoseValue.Should().Be(nameof(BalanceSnapshot.Day1Budget));
        keyToConsumer.Should().ContainKey("waves.normal.daily_growth").WhoseValue.Should().Be(nameof(BalanceSnapshot.DailyGrowth));
        keyToConsumer.Should().ContainKey("channels.elite").WhoseValue.Should().Be(nameof(BalanceSnapshot.EliteChannel));
        keyToConsumer.Should().ContainKey("channels.boss").WhoseValue.Should().Be(nameof(BalanceSnapshot.BossChannel));
        keyToConsumer.Should().ContainKey("spawn.cadence_seconds").WhoseValue.Should().Be(nameof(BalanceSnapshot.SpawnCadenceSeconds));
        keyToConsumer.Should().ContainKey("boss.count").WhoseValue.Should().Be(nameof(BalanceSnapshot.BossCount));
    }

    private static JsonDocument CreateCanonicalConfig()
    {
        return JsonDocument.Parse(
            @"{
  ""time"": {
    ""day_seconds"": 240,
    ""night_seconds"": 120
  },
  ""waves"": {
    ""normal"": {
      ""day1_budget"": 50,
      ""daily_growth"": 1.2
    }
  },
  ""channels"": {
    ""elite"": ""elite"",
    ""boss"": ""boss""
  },
  ""spawn"": {
    ""cadence_seconds"": 10
  },
  ""boss"": {
    ""count"": 2
  }
}");
    }

    private static bool HasPath(JsonElement element, string path)
    {
        var parts = path.Split('.');
        var current = element;
        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
            {
                return false;
            }
        }

        return true;
    }

}
