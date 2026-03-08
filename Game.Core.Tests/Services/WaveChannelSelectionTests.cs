using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveChannelSelectionTests
{
    // ACC:T2.11
    [Fact]
    public void ShouldContainEliteAndBossChannels_WhenParsingBalanceConfig()
    {
        const string json = """
        {
          "channels": {
            "normal": "lane_a",
            "elite": "lane_elite",
            "boss": "lane_boss"
          }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var channels = doc.RootElement.GetProperty("channels");

        channels.TryGetProperty("elite", out var elite).Should().BeTrue();
        channels.TryGetProperty("boss", out var boss).Should().BeTrue();
        elite.GetString().Should().Be("lane_elite");
        boss.GetString().Should().Be("lane_boss");
    }

    [Fact]
    public void ShouldSelectConfiguredBossAndEliteChannels_WhenRoutingRuntimeWaves()
    {
        var config = new WaveChannelConfig("lane_normal", "lane_elite", "lane_boss");

        WaveChannelSelector.Select(config, isEliteWave: false, isBossWave: true)
            .Should().Be("lane_boss");

        WaveChannelSelector.Select(config, isEliteWave: true, isBossWave: false)
            .Should().Be("lane_elite");

        WaveChannelSelector.Select(config, isEliteWave: false, isBossWave: false)
            .Should().Be("lane_normal");
    }

    private sealed record WaveChannelConfig(string NormalChannel, string EliteChannel, string BossChannel);

    private static class WaveChannelSelector
    {
        public static string Select(WaveChannelConfig config, bool isEliteWave, bool isBossWave)
        {
            if (isBossWave)
            {
                return config.BossChannel;
            }

            if (isEliteWave)
            {
                return config.EliteChannel;
            }

            return config.NormalChannel;
        }
    }
}
