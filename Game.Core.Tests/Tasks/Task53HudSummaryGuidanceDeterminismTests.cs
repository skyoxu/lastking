using System;
using FluentAssertions;
using Game.Core.Contracts.Lastking;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task53HudSummaryGuidanceDeterminismTests
{
    // ACC:T53.2
    // ACC:T53.5
    // ACC:T53.8
    [Fact]
    public void ShouldProduceDeterministicContractFedSummaryAndGuidance_WhenInputsAreIdentical()
    {
        var composer = new HudAfterActionComposer();
        var runId = "run-53";
        var reward = new RewardOffered(
            runId,
            DayNumber: 9,
            IsEliteNight: true,
            IsBossNight: false,
            OptionA: "gold+100",
            OptionB: "tech+1",
            OptionC: "unit+tank",
            OfferedAt: DateTimeOffset.Parse("2026-05-02T09:00:00Z"));
        var resources = new ResourcesChanged(
            runId,
            DayNumber: 9,
            Gold: 120,
            Iron: 44,
            PopulationCap: 26,
            ChangedAt: DateTimeOffset.Parse("2026-05-02T09:00:01Z"));
        var tax = new TaxCollected(
            runId,
            DayNumber: 9,
            ResidenceId: "res-1",
            GoldDelta: 15,
            TotalGold: 135,
            CollectedAt: DateTimeOffset.Parse("2026-05-02T09:00:02Z"));
        var tech = new TechApplied(
            runId,
            TechId: "tech_rate_i",
            StatKey: "attack_speed",
            PreviousValue: 100,
            CurrentValue: 110,
            AppliedAt: DateTimeOffset.Parse("2026-05-02T09:00:03Z"));
        var hp = new CastleHpChanged(
            runId,
            DayNumber: 9,
            PreviousHp: 100,
            CurrentHp: 42,
            ChangedAt: DateTimeOffset.Parse("2026-05-02T09:00:04Z"));
        var wave = new WaveSpawned(
            runId,
            DayNumber: 9,
            NightNumber: 4,
            LaneId: "north",
            SpawnCount: 5,
            WaveBudget: 180,
            SpawnedAt: DateTimeOffset.Parse("2026-05-02T09:00:05Z"));
        var first = composer.Compose(new HudAfterActionInputs(
            Outcome: "win",
            DayNumber: 9,
            CastleHp: hp,
            Wave: wave,
            Resources: resources,
            Tax: tax,
            Tech: tech,
            Reward: reward));
        var second = composer.Compose(new HudAfterActionInputs(
            Outcome: "win",
            DayNumber: 9,
            CastleHp: hp,
            Wave: wave,
            Resources: resources,
            Tax: tax,
            Tech: tech,
            Reward: reward));

        second.Should().Be(first);
        first.ResourceText.Should().Be("Resources: gold=120 iron=44 pop=26");
        first.BuildText.Should().Be("Build: tax=15 total_gold=135 residence=res-1");
        first.ProgressionText.Should().Be("Progression: tech=tech_rate_i:attack_speed 100->110 reward=gold+100, tech+1, unit+tank");
        first.OutcomeText.Should().Be("Outcome: win day=9 pressure=warning (hp=42) spawned=5");
        first.PromptText.Should().Be("Prompt: training: reinforce frontline");
    }

    // ACC:T53.8
    [Fact]
    public void ShouldProduceDeterministicLossGuidance_WhenInputsAreIdentical()
    {
        var composer = new HudAfterActionComposer();
        var runId = "run-53-loss";
        var resources = new ResourcesChanged(
            runId,
            DayNumber: 11,
            Gold: 98,
            Iron: 30,
            PopulationCap: 20,
            ChangedAt: DateTimeOffset.Parse("2026-05-02T09:05:01Z"));
        var hp = new CastleHpChanged(
            runId,
            DayNumber: 11,
            PreviousHp: 35,
            CurrentHp: 0,
            ChangedAt: DateTimeOffset.Parse("2026-05-02T09:05:04Z"));
        var wave = new WaveSpawned(
            runId,
            DayNumber: 11,
            NightNumber: 5,
            LaneId: "west",
            SpawnCount: 8,
            WaveBudget: 220,
            SpawnedAt: DateTimeOffset.Parse("2026-05-02T09:05:05Z"));

        var first = composer.Compose(new HudAfterActionInputs(
            Outcome: "loss",
            DayNumber: 11,
            CastleHp: hp,
            Wave: wave,
            Resources: resources,
            Tax: null,
            Tech: null,
            Reward: null));
        var second = composer.Compose(new HudAfterActionInputs(
            Outcome: "loss",
            DayNumber: 11,
            CastleHp: hp,
            Wave: wave,
            Resources: resources,
            Tax: null,
            Tech: null,
            Reward: null));

        second.Should().Be(first);
        first.OutcomeText.Should().Be("Outcome: loss day=11 pressure=critical (hp=0) spawned=8");
        first.PromptText.Should().Be("Prompt: training: reinforce frontline");
    }
}
