using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task38ConfigGovernanceTests
{
    private const string GovernanceMetadataMissingReason = "CFG_GOVERNANCE_METADATA_MISSING";
    private const string GovernancePrerequisiteMissingReason = "CFG_GOVERNANCE_PREREQUISITE_MISSING";
    private const string GovernancePrerequisiteInvalidReason = "CFG_GOVERNANCE_PREREQUISITE_INVALID";
    private const string GovernancePromotionBlockedReason = "CFG_GOVERNANCE_PROMOTION_BLOCKED";

    // ACC:T38.1
    [Fact]
    public void ShouldApplyConfiguredWaveGovernanceValues_WhenEliteBossAndSpawnTuningChange()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var spawnPoints = new[] { "North", "East" };

        var initialLoad = configManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-baseline",
                eliteDay1Budget: 120,
                eliteLimit: 8,
                eliteCost: 20,
                bossDay1Budget: 300,
                bossLimit: 3,
                bossCost: 100,
                spawnCadenceSeconds: 10,
                bossCount: 2),
            "task38-governed-a.json");
        var baselineWave = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: 4107);
        var baselineSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        var reload = configManager.ReloadFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-promoted",
                eliteDay1Budget: 260,
                eliteLimit: 10,
                eliteCost: 15,
                bossDay1Budget: 640,
                bossLimit: 5,
                bossCost: 80,
                spawnCadenceSeconds: 6,
                bossCount: 4),
            "task38-governed-b.json");
        var promotedWave = waveManager.GenerateFromConfig(dayIndex: 4, configManager, seed: 4107);
        var promotedSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        initialLoad.Accepted.Should().BeTrue();
        reload.Accepted.Should().BeTrue();
        BuildChannelSnapshot(promotedWave.ChannelResults[WaveManager.EliteChannel])
            .Should()
            .NotBe(BuildChannelSnapshot(baselineWave.ChannelResults[WaveManager.EliteChannel]));
        BuildChannelSnapshot(promotedWave.ChannelResults[WaveManager.BossChannel])
            .Should()
            .NotBe(BuildChannelSnapshot(baselineWave.ChannelResults[WaveManager.BossChannel]));
        MeasureFirstIntervalSeconds(promotedSpawns).Should().Be(6.0);
        baselineSpawns.Count.Should().NotBe(promotedSpawns.Count);
    }

    // ACC:T38.7
    [Fact]
    public void ShouldReachDifferentTuningOutcomes_WhenOnlyConfigPayloadSwitches()
    {
        var waveManager = new WaveManager();
        var baselineManager = new ConfigManager();
        var promotedManager = new ConfigManager();
        var spawnPoints = new[] { "North", "East", "South" };

        var baselineLoad = baselineManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-config-a",
                normalDay1Budget: 50,
                normalDailyGrowth: "1.2",
                spawnCadenceSeconds: 10,
                bossCount: 2),
            "task38-config-a.json");
        var promotedLoad = promotedManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-config-b",
                normalDay1Budget: 95,
                normalDailyGrowth: "1.35",
                spawnCadenceSeconds: 5,
                bossCount: 5),
            "task38-config-b.json");

        var baselineMetrics = BalanceRuntimeEvaluator.Evaluate(baselineManager.Snapshot, dayIndex: 6);
        var promotedMetrics = BalanceRuntimeEvaluator.Evaluate(promotedManager.Snapshot, dayIndex: 6);
        var baselineBossSpawns = waveManager.GenerateNightSpawnsFromConfig(baselineManager, isBossNight: true, spawnPoints);
        var promotedBossSpawns = waveManager.GenerateNightSpawnsFromConfig(promotedManager, isBossNight: true, spawnPoints);

        baselineLoad.Accepted.Should().BeTrue();
        promotedLoad.Accepted.Should().BeTrue();
        promotedMetrics.WaveBudget.Should().NotBe(baselineMetrics.WaveBudget);
        promotedMetrics.SpawnCadenceSeconds.Should().NotBe(baselineMetrics.SpawnCadenceSeconds);
        promotedMetrics.BossCount.Should().NotBe(baselineMetrics.BossCount);
        BuildSpawnSnapshot(promotedBossSpawns).Should().NotBe(BuildSpawnSnapshot(baselineBossSpawns));
    }

    // ACC:T38.9
    [Fact]
    public void ShouldRejectPromotionCandidate_WhenGovernanceMetadataIsMissing()
    {
        var configManager = new ConfigManager();
        var baseline = configManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-baseline-governed"),
            "task38-baseline-governed.json");

        var result = configManager.ReloadFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-missing-governance",
                includeGovernance: false),
            "task38-missing-governance.json");

        baseline.Accepted.Should().BeTrue();
        result.Accepted.Should().BeFalse("promotion candidates must declare governance metadata before tuning is trusted");
        result.ReasonCodes.Should().Contain(GovernanceMetadataMissingReason);
    }

    // ACC:T38.19
    [Fact]
    public void ShouldRejectPromotionCandidate_WhenPromotionApprovalEvidenceIsBlank()
    {
        var configManager = new ConfigManager();

        var result = configManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-missing-approval",
                approvalTicket: string.Empty,
                soakReportId: "SOAK-38"),
            "task38-missing-approval.json");

        result.Accepted.Should().BeFalse("promotion prerequisites must include approval evidence");
        result.ReasonCodes.Should().Contain(GovernancePrerequisiteMissingReason);
    }

    // ACC:T38.20
    [Fact]
    public void ShouldReturnComparableReasonCodeAndKeepActiveSnapshot_WhenGovernancePrerequisiteIsInvalid()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var spawnPoints = new[] { "North", "East" };

        var initialLoad = configManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-valid-baseline",
                spawnCadenceSeconds: 9,
                bossCount: 2),
            "task38-valid-baseline.json");
        var baselineSnapshot = configManager.Snapshot;
        var baselineSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        var rejectedReload = configManager.ReloadFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-invalid-prerequisite",
                spawnCadenceSeconds: 4,
                bossCount: 5,
                regressionGatePassed: false),
            "task38-invalid-prerequisite.json");
        var afterRejectedReloadSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        initialLoad.Accepted.Should().BeTrue();
        rejectedReload.Accepted.Should().BeFalse();
        rejectedReload.ReasonCodes.Should().Contain(GovernancePrerequisiteInvalidReason);
        configManager.Snapshot.Should().Be(baselineSnapshot);
        BuildSpawnSnapshot(afterRejectedReloadSpawns).Should().Be(BuildSpawnSnapshot(baselineSpawns));
    }

    // ACC:T38.23
    [Fact]
    public void ShouldKeepGovernanceRejectionStableAcrossRepeatedPromotionAttempts_WhenRegressionGateRunsAgain()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var spawnPoints = new[] { "North", "East" };

        var initialLoad = configManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-regression-baseline",
                spawnCadenceSeconds: 8,
                bossCount: 2),
            "task38-regression-baseline.json");
        var baselineWave = waveManager.GenerateFromConfig(dayIndex: 5, configManager, seed: 8821);
        var baselineSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        var firstRejectedReload = configManager.ReloadFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-regression-candidate-a",
                spawnCadenceSeconds: 5,
                regressionGatePassed: false),
            "task38-regression-candidate-a.json");
        var secondRejectedReload = configManager.ReloadFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-regression-candidate-b",
                spawnCadenceSeconds: 4,
                regressionGatePassed: false),
            "task38-regression-candidate-b.json");
        var finalWave = waveManager.GenerateFromConfig(dayIndex: 5, configManager, seed: 8821);
        var finalSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        initialLoad.Accepted.Should().BeTrue();
        firstRejectedReload.Accepted.Should().BeFalse();
        secondRejectedReload.Accepted.Should().BeFalse();
        firstRejectedReload.ReasonCodes.Should().Equal(secondRejectedReload.ReasonCodes);
        BuildWaveSnapshot(finalWave).Should().Be(BuildWaveSnapshot(baselineWave));
        BuildSpawnSnapshot(finalSpawns).Should().Be(BuildSpawnSnapshot(baselineSpawns));
    }

    // ACC:T38.24
    [Fact]
    public void ShouldBlockPromotionAndKeepProductionOutputsUnchanged_WhenGovernanceEnforcementWouldBeWeakened()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var spawnPoints = new[] { "North", "East", "South" };

        var initialLoad = configManager.LoadInitialFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-production-baseline",
                normalDay1Budget: 55,
                normalDailyGrowth: "1.2",
                spawnCadenceSeconds: 10,
                bossCount: 2),
            "task38-production-baseline.json");
        var baselineMetrics = BalanceRuntimeEvaluator.Evaluate(configManager.Snapshot, dayIndex: 5);
        var baselineWave = waveManager.GenerateFromConfig(dayIndex: 5, configManager, seed: 9255);
        var baselineBossSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: true, spawnPoints);

        var weakenedReload = configManager.ReloadFromJson(
            BuildGovernedConfigJson(
                tuningSetId: "task38-unsafe-candidate",
                normalDay1Budget: 160,
                normalDailyGrowth: "1.45",
                spawnCadenceSeconds: 4,
                bossCount: 6,
                includeGovernance: false),
            "task38-unsafe-candidate.json");
        var finalMetrics = BalanceRuntimeEvaluator.Evaluate(configManager.Snapshot, dayIndex: 5);
        var finalWave = waveManager.GenerateFromConfig(dayIndex: 5, configManager, seed: 9255);
        var finalBossSpawns = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: true, spawnPoints);

        initialLoad.Accepted.Should().BeTrue();
        weakenedReload.Accepted.Should().BeFalse();
        weakenedReload.ReasonCodes.Should().Contain(GovernancePromotionBlockedReason);
        finalMetrics.Should().Be(baselineMetrics);
        BuildWaveSnapshot(finalWave).Should().Be(BuildWaveSnapshot(baselineWave));
        BuildSpawnSnapshot(finalBossSpawns).Should().Be(BuildSpawnSnapshot(baselineBossSpawns));
    }

    private static string BuildGovernedConfigJson(
        string tuningSetId,
        int normalDay1Budget = 50,
        string normalDailyGrowth = "1.2",
        int eliteDay1Budget = 120,
        string eliteDailyGrowth = "1.2",
        int eliteLimit = 8,
        int eliteCost = 20,
        int bossDay1Budget = 300,
        string bossDailyGrowth = "1.2",
        int bossLimit = 3,
        int bossCost = 100,
        int spawnCadenceSeconds = 10,
        int bossCount = 2,
        bool includeGovernance = true,
        string governanceSchemaVersion = "1.0.0",
        string approvalTicket = "CAB-38",
        string soakReportId = "SOAK-38",
        bool regressionGatePassed = true)
    {
        var governanceSection = includeGovernance
            ? $$"""
      ,
      "governance": {
        "schema_version": "{{governanceSchemaVersion}}",
        "tuning_set_id": "{{tuningSetId}}",
        "promotion": {
          "approval_ticket": "{{approvalTicket}}",
          "soak_report_id": "{{soakReportId}}",
          "regression_gate_passed": {{regressionGatePassed.ToString().ToLowerInvariant()}}
        }
      }
"""
            : string.Empty;

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
          "battle": { "castle_start_hp": 100 }{{governanceSection}}
        }
        """;
    }

    private static double MeasureFirstIntervalSeconds(IReadOnlyList<NightSpawnEmission> emissions)
    {
        if (emissions.Count < 2)
        {
            throw new InvalidOperationException("At least two emissions are required to measure a spawn interval.");
        }

        return emissions[1].ElapsedSeconds - emissions[0].ElapsedSeconds;
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

    private static string BuildSpawnSnapshot(IReadOnlyList<NightSpawnEmission> emissions)
    {
        return string.Join(
            "|",
            emissions.Select(emission => $"{emission.ElapsedSeconds:0.###}:{emission.SpawnPointId}:{emission.EnemyType}:{emission.EnemyArchetype}"));
    }
}
