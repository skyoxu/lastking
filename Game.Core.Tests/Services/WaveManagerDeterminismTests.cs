using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerDeterminismTests
{
    private const string NormalChannel = "normal";
    private const string EliteChannel = "elite";
    private const string BossChannel = "boss";

    // ACC:T4.1
    [Fact]
    public void ShouldUseConfiguredBudgetPerChannel_WhenGeneratingWaveForAllChannels()
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
    public void ShouldRemainBitwiseEquivalent_WhenDaySeedAndConfigStayUnchanged()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var firstRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 2026);
        var secondRun = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 2026);

        BuildWaveSnapshot(secondRun).Should().Be(BuildWaveSnapshot(firstRun));
    }

    // ACC:T4.3
    [Fact]
    public void ShouldPreserveDeterministicReplayAndIsolatedBudgetComputation_WhenWaveInputsAreFixed()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var tunedConfig = baselineConfig with
        {
            Elite = baselineConfig.Elite with { Day1Budget = baselineConfig.Elite.Day1Budget + 280 },
            Boss = baselineConfig.Boss with { Day1Budget = baselineConfig.Boss.Day1Budget + 500 }
        };

        var baseline = sut.Generate(dayIndex: 4, channelBudgetConfiguration: baselineConfig, seed: 1337);
        var replay = sut.Generate(dayIndex: 4, channelBudgetConfiguration: baselineConfig, seed: 1337);
        var tuned = sut.Generate(dayIndex: 4, channelBudgetConfiguration: tunedConfig, seed: 1337);

        BuildWaveSnapshot(replay).Should().Be(BuildWaveSnapshot(baseline));
        BuildChannelSnapshot(tuned.ChannelResults[NormalChannel])
            .Should().Be(BuildChannelSnapshot(baseline.ChannelResults[NormalChannel]));
    }

    // ACC:T4.7
    [Fact]
    public void ShouldKeepNormalChannelUnchanged_WhenOnlyEliteBudgetChanges()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var tunedConfig = baselineConfig with
        {
            Elite = baselineConfig.Elite with { Day1Budget = baselineConfig.Elite.Day1Budget + 80 }
        };

        var baselineResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: baselineConfig, seed: 1337);
        var tunedResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: tunedConfig, seed: 1337);

        BuildChannelSnapshot(tunedResult.ChannelResults[NormalChannel])
            .Should().Be(BuildChannelSnapshot(baselineResult.ChannelResults[NormalChannel]));
    }

    // ACC:T4.8
    [Fact]
    public void ShouldKeepNormalChannelUnchanged_WhenOnlyBossBudgetChanges()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var tunedConfig = baselineConfig with
        {
            Boss = baselineConfig.Boss with { Day1Budget = baselineConfig.Boss.Day1Budget + 120 }
        };

        var baselineResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: baselineConfig, seed: 1337);
        var tunedResult = sut.Generate(dayIndex: 3, channelBudgetConfiguration: tunedConfig, seed: 1337);

        BuildChannelSnapshot(tunedResult.ChannelResults[NormalChannel])
            .Should().Be(BuildChannelSnapshot(baselineResult.ChannelResults[NormalChannel]));
    }

    // ACC:T4.10
    [Fact]
    public void ShouldProduceStableReplayAndSeedDrift_WhenRunningWithFixedDayAndConfiguration()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var firstRun = sut.Generate(dayIndex: 4, channelBudgetConfiguration: config, seed: 777);
        var replayRun = sut.Generate(dayIndex: 4, channelBudgetConfiguration: config, seed: 777);
        var driftedSeedRun = sut.Generate(dayIndex: 4, channelBudgetConfiguration: config, seed: 778);

        BuildWaveSnapshot(replayRun).Should().Be(BuildWaveSnapshot(firstRun));
        BuildWaveSnapshot(driftedSeedRun).Should().NotBe(BuildWaveSnapshot(firstRun));
    }

    // ACC:T4.11
    [Theory]
    [InlineData(NormalChannel)]
    [InlineData(EliteChannel)]
    [InlineData(BossChannel)]
    public void ShouldChangeOnlyTargetChannelOutput_WhenTuningSingleChannelBudget(string targetChannel)
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

    // ACC:T4.13
    [Fact]
    public void ShouldRejectInvalidTransitionsAndReplayTraceDeterministically_WhenAccountingTransitionIsInvalid()
    {
        var sut = new WaveManager();
        var initialState = CreateAccountingState();

        var negativeSpendAttempt = new AccountingTransitionAttempt(
            SourceChannel: NormalChannel,
            ChargeChannel: NormalChannel,
            Spend: -1);

        var firstNegative = sut.TryApplyAccountingTransition(initialState, negativeSpendAttempt, seed: 77);
        var replayNegative = sut.TryApplyAccountingTransition(initialState, negativeSpendAttempt, seed: 77);

        firstNegative.Accepted.Should().BeFalse();
        replayNegative.Accepted.Should().BeFalse();
        firstNegative.Reason.Should().Be("negative-spend");
        replayNegative.Reason.Should().Be("negative-spend");
        firstNegative.StateAfter.Should().Be(initialState);
        replayNegative.StateAfter.Should().Be(initialState);
        replayNegative.Trace.Should().Be(firstNegative.Trace);

        var overBudgetAttempt = new AccountingTransitionAttempt(
            SourceChannel: NormalChannel,
            ChargeChannel: NormalChannel,
            Spend: 999);

        var overBudget = sut.TryApplyAccountingTransition(initialState, overBudgetAttempt, seed: 77);
        var replayOverBudget = sut.TryApplyAccountingTransition(initialState, overBudgetAttempt, seed: 77);

        overBudget.Accepted.Should().BeFalse();
        replayOverBudget.Accepted.Should().BeFalse();
        overBudget.Reason.Should().Be("over-budget-spend");
        replayOverBudget.Reason.Should().Be("over-budget-spend");
        overBudget.StateAfter.Should().Be(initialState);
        replayOverBudget.StateAfter.Should().Be(initialState);
        replayOverBudget.Trace.Should().Be(overBudget.Trace);

        var crossChannelAttempt = new AccountingTransitionAttempt(
            SourceChannel: NormalChannel,
            ChargeChannel: EliteChannel,
            Spend: 5);

        var crossChannel = sut.TryApplyAccountingTransition(initialState, crossChannelAttempt, seed: 77);
        var replayCrossChannel = sut.TryApplyAccountingTransition(initialState, crossChannelAttempt, seed: 77);

        crossChannel.Accepted.Should().BeFalse();
        replayCrossChannel.Accepted.Should().BeFalse();
        crossChannel.Reason.Should().Be("cross-channel-charge");
        replayCrossChannel.Reason.Should().Be("cross-channel-charge");
        crossChannel.StateAfter.Should().Be(initialState);
        replayCrossChannel.StateAfter.Should().Be(initialState);
        replayCrossChannel.Trace.Should().Be(crossChannel.Trace);
    }

    private static ChannelBudgetConfiguration CreateDefaultConfig()
    {
        return new ChannelBudgetConfiguration(
            Normal: new ChannelRule(Day1Budget: 50, DailyGrowth: 1.2m, ChannelLimit: 20, CostPerEnemy: 10),
            Elite: new ChannelRule(Day1Budget: 120, DailyGrowth: 1.2m, ChannelLimit: 8, CostPerEnemy: 20),
            Boss: new ChannelRule(Day1Budget: 300, DailyGrowth: 1.2m, ChannelLimit: 3, CostPerEnemy: 100));
    }

    private static WaveAccountingState CreateAccountingState()
    {
        return new WaveAccountingState(
            Normal: new ChannelAccounting(InputBudget: 50, Allocated: 40, Spent: 15, Remaining: 25),
            Elite: new ChannelAccounting(InputBudget: 120, Allocated: 80, Spent: 30, Remaining: 50),
            Boss: new ChannelAccounting(InputBudget: 300, Allocated: 200, Spent: 90, Remaining: 110));
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
