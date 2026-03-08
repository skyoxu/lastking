using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class ConfigManagerParsingTests
{
    // ACC:T2.15
    [Fact]
    public void ShouldParseDayNightDurations_WhenReadingCanonicalConfigJson()
    {
        using var document = CreateCanonicalConfigDocument();
        var manager = new ConfigManager();
        var result = manager.LoadInitialFromJson(document.RootElement.GetRawText(), "res://Config/balance.json");

        var daySeconds = result.Snapshot.DaySeconds;
        var nightSeconds = result.Snapshot.NightSeconds;

        daySeconds.Should().Be(240);
        nightSeconds.Should().Be(120);
        (daySeconds + nightSeconds).Should().Be(360);
    }

    // ACC:T2.8
    [Fact]
    public void ShouldContainRequiredBalancingContractKeys_WhenBuildingCanonicalConfigDocument()
    {
        using var document = CreateCanonicalConfigDocument();
        var root = document.RootElement;

        var requiredPaths = new[]
        {
            "time.day_seconds",
            "time.night_seconds",
            "waves.normal.day1_budget",
            "waves.normal.daily_growth",
            "channels.elite",
            "channels.boss",
            "spawn.cadence_seconds",
            "boss.count"
        };

        foreach (var path in requiredPaths)
        {
            HasPath(root, path).Should().BeTrue($"required config key '{path}' should exist");
        }

        var manager = new ConfigManager();
        var parsed = manager.LoadInitialFromJson(root.GetRawText(), "res://Config/balance.json");
        parsed.Accepted.Should().BeTrue();
        parsed.Snapshot.Day1Budget.Should().Be(50);
        parsed.Snapshot.DailyGrowth.Should().Be(1.2m);
        parsed.Snapshot.EliteChannel.Should().Be("elite");
        parsed.Snapshot.BossChannel.Should().Be("boss");
        parsed.Snapshot.SpawnCadenceSeconds.Should().Be(10);
        parsed.Snapshot.BossCount.Should().Be(2);
    }

    private static bool HasPath(JsonElement root, string dottedPath)
    {
        var current = root;
        foreach (var segment in dottedPath.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!current.TryGetProperty(segment, out var next))
            {
                return false;
            }

            current = next;
        }

        return true;
    }

    private static JsonDocument CreateCanonicalConfigDocument()
    {
        const string json = @"
{
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
}";

        return JsonDocument.Parse(json);
    }
}
