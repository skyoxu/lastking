using System;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveSeedDeterminismTests
{
    // ACC:T38.2
    [Fact]
    public void ShouldFollowActiveConfigAndSeed_WhenGeneratingWaveFromConfigManager()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var configuredSeed = 4107;

        var initialLoad = configManager.LoadInitialFromJson(
            BuildConfigJson(normalDay1Budget: 50, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-config-a.json");
        var firstRun = waveManager.GenerateFromConfig(dayIndex: 3, configManager, seed: configuredSeed);
        var replayRun = waveManager.GenerateFromConfig(dayIndex: 3, configManager, seed: configuredSeed);

        var reload = configManager.ReloadFromJson(
            BuildConfigJson(normalDay1Budget: 95, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-config-b.json");
        var changedConfigRun = waveManager.GenerateFromConfig(dayIndex: 3, configManager, seed: configuredSeed);

        initialLoad.Accepted.Should().BeTrue();
        reload.Accepted.Should().BeTrue();
        BuildWaveSnapshot(replayRun).Should().Be(BuildWaveSnapshot(firstRun));
        BuildWaveSnapshot(changedConfigRun).Should().NotBe(BuildWaveSnapshot(firstRun));
    }

    // ACC:T38.3
    [Fact]
    public void ShouldChangeGovernedWaveChannels_WhenConfigMigratesAwayFromHardcodedEliteAndBossValues()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var configuredSeed = 4107;

        var initialLoad = configManager.LoadInitialFromJson(
            BuildConfigJson(normalDay1Budget: 50, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-governance-a.json");
        var baselineRun = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: configuredSeed);

        var reload = configManager.ReloadFromJson(
            BuildConfigJson(
                normalDay1Budget: 50,
                eliteDay1Budget: 260,
                bossDay1Budget: 640,
                eliteLimit: 10,
                eliteCost: 15,
                bossLimit: 5,
                bossCost: 80),
            "task38-governance-b.json");
        var governedRun = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: configuredSeed);

        initialLoad.Accepted.Should().BeTrue();
        reload.Accepted.Should().BeTrue();
        BuildWaveSnapshot(governedRun).Should().NotBe(BuildWaveSnapshot(baselineRun));
    }

    // ACC:T38.6
    [Fact]
    public void ShouldRemainDeterministic_WhenSameSeedIsReplayedAgainstSameSpawnConfig()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var configuredSeed = 9125;

        var loadResult = configManager.LoadInitialFromJson(
            BuildConfigJson(normalDay1Budget: 72, eliteDay1Budget: 140, bossDay1Budget: 320),
            "task38-deterministic.json");
        var firstRun = waveManager.GenerateFromConfig(dayIndex: 5, configManager, seed: configuredSeed);
        var secondRun = waveManager.GenerateFromConfig(dayIndex: 5, configManager, seed: configuredSeed);

        loadResult.Accepted.Should().BeTrue();
        BuildWaveSnapshot(secondRun).Should().Be(BuildWaveSnapshot(firstRun));
    }

    // ACC:T38.8
    [Fact]
    public void ShouldTrackConfigDeltaAndKeepFixedSeedRerunsStable_WhenDistinctValidConfigSetsAreActivated()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var configuredSeed = 2511;

        var initialLoad = configManager.LoadInitialFromJson(
            BuildConfigJson(normalDay1Budget: 50, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-config-set-a.json");
        var configAFirstRun = waveManager.GenerateFromConfig(dayIndex: 2, configManager, seed: configuredSeed);
        var configAReplayRun = waveManager.GenerateFromConfig(dayIndex: 2, configManager, seed: configuredSeed);

        var reload = configManager.ReloadFromJson(
            BuildConfigJson(normalDay1Budget: 90, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-config-set-b.json");
        var configBRun = waveManager.GenerateFromConfig(dayIndex: 2, configManager, seed: configuredSeed);

        initialLoad.Accepted.Should().BeTrue();
        reload.Accepted.Should().BeTrue();
        BuildWaveSnapshot(configAReplayRun).Should().Be(BuildWaveSnapshot(configAFirstRun));
        BuildWaveSnapshot(configBRun).Should().NotBe(BuildWaveSnapshot(configAFirstRun));
    }

    // ACC:T38.12
    [Fact]
    public void ShouldChangeWaveComposition_WhenOnlySeedChangesUnderFixedConfig()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();

        var loadResult = configManager.LoadInitialFromJson(
            BuildConfigJson(normalDay1Budget: 50, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-seed-fixed-config.json");
        var firstSeedRun = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: 6001);
        var replaySeedRun = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: 6001);
        var changedSeedRun = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: 6002);

        loadResult.Accepted.Should().BeTrue();
        BuildWaveSnapshot(replaySeedRun).Should().Be(BuildWaveSnapshot(firstSeedRun));
        BuildWaveSnapshot(changedSeedRun).Should().NotBe(BuildWaveSnapshot(firstSeedRun));
    }

    // ACC:T38.14
    [Fact]
    public void ShouldRestoreOriginalWaveOutputs_WhenConfigSwitchesFromAToBToAWithFixedSeed()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var configuredSeed = 7123;
        var configA = BuildConfigJson(normalDay1Budget: 50, eliteDay1Budget: 120, bossDay1Budget: 300);
        var configB = BuildConfigJson(
            normalDay1Budget: 50,
            eliteDay1Budget: 260,
            bossDay1Budget: 640,
            eliteLimit: 10,
            eliteCost: 15,
            bossLimit: 5,
            bossCost: 80);

        var initialLoad = configManager.LoadInitialFromJson(configA, "task38-restore-a.json");
        var firstRunA = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: configuredSeed);

        var reloadToB = configManager.ReloadFromJson(configB, "task38-restore-b.json");
        var runB = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: configuredSeed);

        var reloadBackToA = configManager.ReloadFromJson(configA, "task38-restore-a-return.json");
        var secondRunA = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: configuredSeed);

        initialLoad.Accepted.Should().BeTrue();
        reloadToB.Accepted.Should().BeTrue();
        reloadBackToA.Accepted.Should().BeTrue();
        BuildWaveSnapshot(runB).Should().NotBe(BuildWaveSnapshot(firstRunA));
        BuildWaveSnapshot(secondRunA).Should().Be(BuildWaveSnapshot(firstRunA));
    }

    [Fact]
    public void ShouldKeepPreviousWaveOutputs_WhenRejectedReloadLeavesActiveSnapshotUnchanged()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var configuredSeed = 3109;

        var initialLoad = configManager.LoadInitialFromJson(
            BuildConfigJson(normalDay1Budget: 50, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-stable.json");
        var beforeRejectedReload = waveManager.GenerateFromConfig(dayIndex: 3, configManager, seed: configuredSeed);

        var rejectedReload = configManager.ReloadFromJson(
            BuildConfigJson(normalDay1Budget: -1, eliteDay1Budget: 120, bossDay1Budget: 300),
            "task38-invalid.json");
        var afterRejectedReload = waveManager.GenerateFromConfig(dayIndex: 3, configManager, seed: configuredSeed);

        initialLoad.Accepted.Should().BeTrue();
        rejectedReload.Accepted.Should().BeFalse();
        rejectedReload.ReasonCodes.Should().Contain(ConfigManager.OutOfRangeReason);
        BuildWaveSnapshot(afterRejectedReload).Should().Be(BuildWaveSnapshot(beforeRejectedReload));
    }

    private static string BuildConfigJson(
        int normalDay1Budget,
        int eliteDay1Budget,
        int bossDay1Budget,
        string normalDailyGrowth = "1.2",
        string eliteDailyGrowth = "1.2",
        string bossDailyGrowth = "1.2",
        int eliteLimit = 8,
        int eliteCost = 20,
        int bossLimit = 3,
        int bossCost = 100,
        int spawnCadenceSeconds = 10,
        int bossCount = 2)
    {
        return $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": 120 },
          "waves": {
            "normal": { "day1_budget": {{normalDay1Budget}}, "daily_growth": {{normalDailyGrowth}} },
            "elite": {
              "day1_budget": {{eliteDay1Budget}},
              "daily_growth": {{eliteDailyGrowth}},
              "channel_limit": {{eliteLimit}},
              "cost_per_enemy": {{eliteCost}}
            },
            "boss": {
              "day1_budget": {{bossDay1Budget}},
              "daily_growth": {{bossDailyGrowth}},
              "channel_limit": {{bossLimit}},
              "cost_per_enemy": {{bossCost}}
            }
          },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": { "cadence_seconds": {{spawnCadenceSeconds}} },
          "boss": { "count": {{bossCount}} },
          "battle": { "castle_start_hp": 100 }
        }
        """;
    }

    private static string BuildWaveSnapshot(WaveResult waveResult)
    {
        var channelSnapshots = waveResult.ChannelResults
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{BuildChannelSnapshot(pair.Value)}");

        return $"{waveResult.DayIndex}|{waveResult.Seed}|{string.Join("|", channelSnapshots)}";
    }

    private static string BuildChannelSnapshot(ChannelWaveResult channelWaveResult)
    {
        var audit = channelWaveResult.Audit;
        return $"{audit.InputBudget},{audit.Allocated},{audit.Spent},{audit.Remaining}|{string.Join(",", channelWaveResult.SpawnOrder)}";
    }
}
