using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class SpawnScheduleFromConfigTests
{
    // ACC:T2.10
    [Fact]
    public void ShouldParseSpawnCadenceAndBossCount_WhenReadingBalancingConfig()
    {
        using var document = JsonDocument.Parse("""
        {
          "spawn": { "cadence_seconds": 10 },
          "boss": { "count": 2 }
        }
        """);

        var parsed = TryReadSpawnBalance(document.RootElement, out var balance);

        parsed.Should().BeTrue();
        balance.SpawnCadenceSeconds.Should().Be(10);
        balance.BossCount.Should().Be(2);
    }

    [Fact]
    public void ShouldReturnFalse_WhenBalancingKeysAreMissing()
    {
        using var document = JsonDocument.Parse("""
        {
          "spawn": { "cadence_seconds": 10 }
        }
        """);

        var parsed = TryReadSpawnBalance(document.RootElement, out var balance);

        parsed.Should().BeFalse();
        balance.Should().Be(SpawnBalance.Empty);
    }

    private static bool TryReadSpawnBalance(JsonElement root, out SpawnBalance balance)
    {
        balance = SpawnBalance.Empty;

        if (!root.TryGetProperty("spawn", out var spawn)
            || !spawn.TryGetProperty("cadence_seconds", out var cadence)
            || cadence.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        if (!root.TryGetProperty("boss", out var boss)
            || !boss.TryGetProperty("count", out var count)
            || count.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        balance = new SpawnBalance(cadence.GetInt32(), count.GetInt32());
        return true;
    }

    private readonly record struct SpawnBalance(int SpawnCadenceSeconds, int BossCount)
    {
        public static SpawnBalance Empty => new(0, 0);
    }
}
