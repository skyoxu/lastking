using System;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;
using Xunit;

namespace Game.Core.Tests.Contracts.Lastking;

public class LastkingContractsTests
{
    [Fact]
    public void Event_types_should_match_eventtypes_ssot()
    {
        DayStarted.EventType.Should().Be(EventTypes.LastkingDayStarted);
        NightStarted.EventType.Should().Be(EventTypes.LastkingNightStarted);
        WaveSpawned.EventType.Should().Be(EventTypes.LastkingWaveSpawned);
        CastleHpChanged.EventType.Should().Be(EventTypes.LastkingCastleHpChanged);
        RewardOffered.EventType.Should().Be(EventTypes.LastkingRewardOffered);
        SaveAutosaved.EventType.Should().Be(EventTypes.LastkingSaveAutosaved);
    }

    [Fact]
    public void Event_records_should_keep_constructor_values()
    {
        var now = DateTimeOffset.UtcNow;
        var dayStarted = new DayStarted("run-1", 1, now);
        var waveSpawned = new WaveSpawned("run-1", 1, 1, "lane-left", 50, 1200, now);
        var rewardOffered = new RewardOffered("run-1", 5, true, false, "tech+3", "gold+600", "unit+tank", now);

        dayStarted.RunId.Should().Be("run-1");
        dayStarted.DayNumber.Should().Be(1);
        dayStarted.StartedAt.Should().Be(now);

        waveSpawned.LaneId.Should().Be("lane-left");
        waveSpawned.SpawnCount.Should().Be(50);
        waveSpawned.WaveBudget.Should().Be(1200);

        rewardOffered.IsEliteNight.Should().BeTrue();
        rewardOffered.IsBossNight.Should().BeFalse();
        rewardOffered.OptionA.Should().Be("tech+3");
    }

    [Fact]
    public void Dto_records_should_be_constructible()
    {
        var computedAt = DateTimeOffset.UtcNow;
        var waveBudget = new WaveBudgetDto(5, 5, 2500, 600, 800, computedAt);
        var rewardOffer = new RewardOfferDto(5, true, false, "artifact", "gold", "tech");

        waveBudget.DayNumber.Should().Be(5);
        waveBudget.NormalBudget.Should().Be(2500);
        waveBudget.ComputedAt.Should().Be(computedAt);

        rewardOffer.DayNumber.Should().Be(5);
        rewardOffer.IsEliteNight.Should().BeTrue();
        rewardOffer.OptionC.Should().Be("tech");
    }

    [Fact]
    public void Interface_contract_should_compile_with_stub_implementation()
    {
        IWaveBudgetPolicy policy = new StubWaveBudgetPolicy();

        var result = policy.Compute(dayNumber: 3, nightNumber: 3, isEliteNight: false, isBossNight: false);

        result.DayNumber.Should().Be(3);
        result.NightNumber.Should().Be(3);
        result.NormalBudget.Should().Be(1800);
    }

    private sealed class StubWaveBudgetPolicy : IWaveBudgetPolicy
    {
        public WaveBudgetDto Compute(int dayNumber, int nightNumber, bool isEliteNight, bool isBossNight)
        {
            return new WaveBudgetDto(
                DayNumber: dayNumber,
                NightNumber: nightNumber,
                NormalBudget: 1800,
                EliteBudget: isEliteNight ? 600 : 0,
                BossBudget: isBossNight ? 1200 : 0,
                ComputedAt: DateTimeOffset.UtcNow
            );
        }
    }
}
