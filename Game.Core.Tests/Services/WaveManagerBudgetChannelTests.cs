using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerBudgetChannelTests
{
    private const string NormalChannel = "normal";
    private const string EliteChannel = "elite";
    private const string BossChannel = "boss";

    // ACC:T4.1
    // ACC:T43.1
    [Fact]
    public void ShouldProduceNormalEliteBossResultsFromConfiguredChannelBudgets_WhenGeneratingWave()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var result = sut.Generate(dayIndex: 1, channelBudgetConfiguration: config, seed: 1337);

        result.ChannelResults.Keys.Should().BeEquivalentTo(new[] { NormalChannel, EliteChannel, BossChannel });
        result.ChannelResults[NormalChannel].Audit.InputBudget.Should().Be(50);
        result.ChannelResults[EliteChannel].Audit.InputBudget.Should().Be(120);
        result.ChannelResults[BossChannel].Audit.InputBudget.Should().Be(300);
    }

    // ACC:T4.2
    [Fact]
    public void ShouldReturnBitwiseEquivalentOutputs_WhenBudgetChannelInputsAreUnchanged()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var firstRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 2026);
        var secondRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 2026);

        BuildWaveSnapshot(secondRun).Should().Be(BuildWaveSnapshot(firstRun));
    }

    // ACC:T4.4
    [Fact]
    public void ShouldDeriveWaveOutputsFromDayConfigAndSeed_WhenInputsChangeIndependently()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var changedConfig = baselineConfig with
        {
            Normal = baselineConfig.Normal with { Day1Budget = 90 }
        };

        var baselineRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: baselineConfig, seed: 8080);
        var sameInputsRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: baselineConfig, seed: 8080);
        var changedDayRun = sut.Generate(dayIndex: 3, channelBudgetConfiguration: baselineConfig, seed: 8080);
        var changedConfigRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: changedConfig, seed: 8080);
        var changedSeedRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: baselineConfig, seed: 8081);

        BuildWaveSnapshot(sameInputsRun).Should().Be(BuildWaveSnapshot(baselineRun));
        BuildWaveSnapshot(changedDayRun).Should().NotBe(BuildWaveSnapshot(baselineRun));
        BuildWaveSnapshot(changedConfigRun).Should().NotBe(BuildWaveSnapshot(baselineRun));
        BuildWaveSnapshot(changedSeedRun).Should().NotBe(BuildWaveSnapshot(baselineRun));
    }

    // ACC:T4.5
    // ACC:T43.4
    [Fact]
    public void ShouldComputeNormalBudgetAs50_WhenUsingDefaultDay1Configuration()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var result = sut.Generate(dayIndex: 1, channelBudgetConfiguration: config, seed: 17);

        result.ChannelResults[NormalChannel].Audit.InputBudget.Should().Be(50);
    }

    // ACC:T4.6
    [Theory]
    [InlineData(2, 60)]
    [InlineData(3, 72)]
    [InlineData(4, 86)]
    public void ShouldApply120PercentGrowthForNormalBudget_WhenDayIndexIsAfterDay1(int dayIndex, int expectedBudget)
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var result = sut.Generate(dayIndex, config, seed: 73);

        result.ChannelResults[NormalChannel].Audit.InputBudget.Should().Be(expectedBudget);
    }

    // ACC:T4.7
    [Fact]
    public void ShouldKeepNormalChannelBudgetAndSpawnUnchanged_WhenOnlyEliteBudgetChanges()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var tunedConfig = baselineConfig with
        {
            Elite = baselineConfig.Elite with { Day1Budget = baselineConfig.Elite.Day1Budget + 80 }
        };

        var baselineResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: baselineConfig, seed: 1337);
        var tunedResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: tunedConfig, seed: 1337);

        tunedResult.ChannelResults[NormalChannel].Audit.InputBudget
            .Should().Be(baselineResult.ChannelResults[NormalChannel].Audit.InputBudget);
        BuildChannelSnapshot(tunedResult.ChannelResults[NormalChannel])
            .Should().Be(BuildChannelSnapshot(baselineResult.ChannelResults[NormalChannel]));
    }

    // ACC:T4.8
    [Fact]
    public void ShouldKeepNormalChannelBudgetAndSpawnUnchanged_WhenOnlyBossBudgetChanges()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var tunedConfig = baselineConfig with
        {
            Boss = baselineConfig.Boss with { Day1Budget = baselineConfig.Boss.Day1Budget + 120 }
        };

        var baselineResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: baselineConfig, seed: 1337);
        var tunedResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: tunedConfig, seed: 1337);

        tunedResult.ChannelResults[NormalChannel].Audit.InputBudget
            .Should().Be(baselineResult.ChannelResults[NormalChannel].Audit.InputBudget);
        BuildChannelSnapshot(tunedResult.ChannelResults[NormalChannel])
            .Should().Be(BuildChannelSnapshot(baselineResult.ChannelResults[NormalChannel]));
    }

    // ACC:T4.9
    [Fact]
    public void ShouldNotExceedChannelBudgetOrLimits_WhenAllocatingWaveSpend()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var result = sut.Generate(dayIndex: 5, channelBudgetConfiguration: config, seed: 9001);

        foreach (var channelName in new[] { NormalChannel, EliteChannel, BossChannel })
        {
            var channelResult = result.ChannelResults[channelName];
            var channelRule = config.GetRule(channelName);

            channelResult.Audit.Allocated.Should().BeLessOrEqualTo(channelResult.Audit.InputBudget);
            channelResult.Audit.Spent.Should().BeLessOrEqualTo(channelResult.Audit.Allocated);
            channelResult.Audit.Remaining.Should().Be(channelResult.Audit.Allocated - channelResult.Audit.Spent);
            channelResult.SpawnOrder.Count.Should().BeLessOrEqualTo(channelRule.ChannelLimit);
        }
    }

    // ACC:T4.11
    // ACC:T43.6
    [Theory]
    [InlineData(NormalChannel)]
    [InlineData(EliteChannel)]
    [InlineData(BossChannel)]
    public void ShouldChangeOnlyTargetedChannelOutput_WhenTuningSingleChannelBudget(string targetChannel)
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var baselineBudget = baselineConfig.GetRule(targetChannel).Day1Budget;
        var tunedConfig = TuneDay1Budget(baselineConfig, targetChannel, baselineBudget + 40);

        var baselineResult = sut.Generate(dayIndex: 2, channelBudgetConfiguration: baselineConfig, seed: 2222);
        var tunedResult = sut.Generate(dayIndex: 2, channelBudgetConfiguration: tunedConfig, seed: 2222);

        foreach (var channelName in new[] { NormalChannel, EliteChannel, BossChannel })
        {
            var baselineSnapshot = BuildChannelSnapshot(baselineResult.ChannelResults[channelName]);
            var tunedSnapshot = BuildChannelSnapshot(tunedResult.ChannelResults[channelName]);

            if (channelName == targetChannel)
            {
                tunedSnapshot.Should().NotBe(baselineSnapshot);
            }
            else
            {
                tunedSnapshot.Should().Be(baselineSnapshot);
            }
        }
    }

    // ACC:T4.17
    [Fact]
    public void ShouldTakeEliteInputBudgetFromEliteConfiguration_WhenDay1IsGenerated()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig() with
        {
            Elite = new ChannelRule(
                Day1Budget: 260,
                DailyGrowth: 1.0m,
                ChannelLimit: 8,
                CostPerEnemy: 20)
        };

        var result = sut.Generate(dayIndex: 1, channelBudgetConfiguration: config, seed: 1401);

        result.ChannelResults[EliteChannel].Audit.InputBudget.Should().Be(260);
        result.ChannelResults[NormalChannel].Audit.InputBudget.Should().Be(50);
        result.ChannelResults[BossChannel].Audit.InputBudget.Should().Be(300);
    }

    // ACC:T4.18
    [Fact]
    public void ShouldTakeBossInputBudgetFromBossConfiguration_WhenDay1IsGenerated()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig() with
        {
            Boss = new ChannelRule(
                Day1Budget: 640,
                DailyGrowth: 1.0m,
                ChannelLimit: 3,
                CostPerEnemy: 100)
        };

        var result = sut.Generate(dayIndex: 1, channelBudgetConfiguration: config, seed: 1402);

        result.ChannelResults[BossChannel].Audit.InputBudget.Should().Be(640);
        result.ChannelResults[NormalChannel].Audit.InputBudget.Should().Be(50);
        result.ChannelResults[EliteChannel].Audit.InputBudget.Should().Be(120);
    }

    // ACC:T4.16
    [Theory]
    [InlineData(0, 10)]
    [InlineData(9, 10)]
    public void ShouldEmitNoSpawnsAndKeepSpentUnchanged_WhenChannelBudgetIsZeroOrBelowMinimumCost(int day1Budget, int costPerEnemy)
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig() with
        {
            Normal = new ChannelRule(
                Day1Budget: day1Budget,
                DailyGrowth: 1.0m,
                ChannelLimit: 20,
                CostPerEnemy: costPerEnemy)
        };

        var result = sut.Generate(dayIndex: 1, channelBudgetConfiguration: config, seed: 3001);
        var normal = result.ChannelResults[NormalChannel];

        normal.Audit.InputBudget.Should().Be(day1Budget);
        normal.Audit.Allocated.Should().Be(day1Budget);
        normal.Audit.Spent.Should().Be(0);
        normal.Audit.Remaining.Should().Be(day1Budget);
        normal.SpawnOrder.Should().BeEmpty();
    }

    // ACC:T4.12
    [Fact]
    public void ShouldEmitAuditableInputAllocatedSpentRemainingEvidence_WhenWaveIsGenerated()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var result = sut.Generate(dayIndex: 4, channelBudgetConfiguration: config, seed: 555);

        result.ChannelResults.Keys.Should().BeEquivalentTo(new[] { NormalChannel, EliteChannel, BossChannel });

        foreach (var channelName in new[] { NormalChannel, EliteChannel, BossChannel })
        {
            var audit = result.ChannelResults[channelName].Audit;
            audit.InputBudget.Should().BeGreaterOrEqualTo(0);
            audit.Allocated.Should().BeGreaterOrEqualTo(0);
            audit.Spent.Should().BeGreaterOrEqualTo(0);
            audit.Remaining.Should().BeGreaterOrEqualTo(0);
            audit.Remaining.Should().Be(audit.Allocated - audit.Spent);
        }
    }

    [Fact]
    public void ShouldRefuseWaveGeneration_WhenDayIndexIsLessThanOne()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();
        var beforeConfig = config;

        Action act = () => sut.Generate(dayIndex: 0, channelBudgetConfiguration: config, seed: 7);

        act.Should().Throw<ArgumentOutOfRangeException>();
        config.Should().Be(beforeConfig);
    }

    private static ChannelBudgetConfiguration CreateDefaultConfig()
    {
        return new ChannelBudgetConfiguration(
            Normal: new ChannelRule(Day1Budget: 50, DailyGrowth: 1.2m, ChannelLimit: 20, CostPerEnemy: 10),
            Elite: new ChannelRule(Day1Budget: 120, DailyGrowth: 1.2m, ChannelLimit: 8, CostPerEnemy: 20),
            Boss: new ChannelRule(Day1Budget: 300, DailyGrowth: 1.2m, ChannelLimit: 3, CostPerEnemy: 100));
    }

    private static ChannelBudgetConfiguration TuneDay1Budget(ChannelBudgetConfiguration config, string channelName, int day1Budget)
    {
        return channelName switch
        {
            NormalChannel => config with { Normal = config.Normal with { Day1Budget = day1Budget } },
            EliteChannel => config with { Elite = config.Elite with { Day1Budget = day1Budget } },
            BossChannel => config with { Boss = config.Boss with { Day1Budget = day1Budget } },
            _ => throw new ArgumentOutOfRangeException(nameof(channelName), channelName, "Unknown channel.")
        };
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
