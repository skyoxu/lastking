using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Contracts.Lastking;
using Game.Core.Services.Reward;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RewardManagerBehaviorTests
{
    // ACC:T18.1
    [Fact]
    [Trait("acceptance", "ACC:T18.1")]
    public void ShouldTriggerExactlyOncePerNight_WhenSameTransitionIsProcessedMultipleTimes()
    {
        var rewardPools = CreateRewardPools();
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var firstResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 1,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-1-night-entry");
        var secondResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 1,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-1-night-entry");

        firstResult.Triggered.Should().BeTrue();
        secondResult.Triggered.Should().BeFalse();
        sut.TriggerCount.Should().Be(1);
    }

    // ACC:T18.3
    [Fact]
    [Trait("acceptance", "ACC:T18.3")]
    public void ShouldTriggerOnNightEntryAndUseActiveNightType_WhenTransitionMovesIntoNight()
    {
        var elitePool = new[] { "elite-a", "elite-b", "elite-c", "elite-d" };
        var rewardPools = CreateRewardPools(elite: elitePool);
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var dayResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 2,
            enteredNight: false,
            isEliteNight: true,
            isBossNight: false,
            transitionToken: "day-2-day-loop");
        var nightResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 2,
            enteredNight: true,
            isEliteNight: true,
            isBossNight: false,
            transitionToken: "day-2-night-entry");

        dayResult.Triggered.Should().BeFalse();
        nightResult.Triggered.Should().BeTrue();
        nightResult.ActiveNightType.Should().Be(NightType.Elite);
        ExtractChoices(nightResult.OfferedEvent!).Should().OnlyContain(choice => elitePool.Contains(choice));
    }

    // ACC:T18.4
    [Fact]
    [Trait("acceptance", "ACC:T18.4")]
    public void ShouldOfferExactlyThreeChoicesFromActivePool_WhenPoolHasAtLeastThreeEntries()
    {
        var normalPool = new[] { "normal-a", "normal-b", "normal-c", "normal-d" };
        var rewardPools = CreateRewardPools(normal: normalPool);
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var result = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 3,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-3-night-entry");

        result.Triggered.Should().BeTrue();
        result.Choices.Should().HaveCount(3);
        result.OfferedEvent.Should().NotBeNull();
        ExtractChoices(result.OfferedEvent!).Should().HaveCount(3);
        ExtractChoices(result.OfferedEvent!).Should().OnlyContain(choice => normalPool.Contains(choice));
    }

    // ACC:T18.5
    [Fact]
    [Trait("acceptance", "ACC:T18.5")]
    public void ShouldGrantFallbackGoldExactlyOnceAndOfferNoChoices_WhenActivePoolIsEmpty()
    {
        var rewardPools = CreateRewardPools(boss: Array.Empty<string>());
        var sut = CreateSut(rewardPools, fallbackGold: 350);

        var result = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 4,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: true,
            transitionToken: "day-4-night-entry");

        result.Triggered.Should().BeTrue();
        result.Choices.Should().BeEmpty();
        result.OfferedEvent.Should().BeNull();
        result.GrantedFallbackGold.Should().Be(350);
        sut.FallbackGoldGranted.Should().Be(350);
    }

    // ACC:T18.8
    [Fact]
    [Trait("acceptance", "ACC:T18.8")]
    public void ShouldNotGrantFallbackGold_WhenChoicesAreGenerated()
    {
        var rewardPools = CreateRewardPools();
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var result = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 5,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-5-night-entry");

        result.Choices.Should().HaveCount(3);
        result.OfferedEvent.Should().NotBeNull();
        result.GrantedFallbackGold.Should().Be(0);
        sut.FallbackGoldGranted.Should().Be(0);
    }

    // ACC:T18.11
    [Fact]
    [Trait("acceptance", "ACC:T18.11")]
    public void ShouldUseCurrentNightTypePerTrigger_WhenConsecutiveNightsHaveDifferentTypes()
    {
        var normalPool = new[] { "normal-1", "normal-2", "normal-3" };
        var elitePool = new[] { "elite-1", "elite-2", "elite-3" };
        var bossPool = new[] { "boss-1", "boss-2", "boss-3" };
        var rewardPools = CreateRewardPools(normalPool, elitePool, bossPool);
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var normalResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 6,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-6-night-entry");
        var eliteResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 7,
            enteredNight: true,
            isEliteNight: true,
            isBossNight: false,
            transitionToken: "day-7-night-entry");
        var bossResult = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 8,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: true,
            transitionToken: "day-8-night-entry");

        ExtractChoices(normalResult.OfferedEvent!).Should().OnlyContain(choice => normalPool.Contains(choice));
        ExtractChoices(eliteResult.OfferedEvent!).Should().OnlyContain(choice => elitePool.Contains(choice));
        ExtractChoices(bossResult.OfferedEvent!).Should().OnlyContain(choice => bossPool.Contains(choice));
        sut.TriggerCount.Should().Be(3);
    }

    // ACC:T18.12
    [Fact]
    [Trait("acceptance", "ACC:T18.12")]
    public void ShouldKeepChoicesWithinActivePoolOnly_WhenGeneratingSingleNightOffer()
    {
        var normalPool = new[] { "normal-1", "normal-2", "normal-3" };
        var elitePool = new[] { "elite-1", "elite-2", "elite-3" };
        var bossPool = new[] { "boss-1", "boss-2", "boss-3" };
        var rewardPools = CreateRewardPools(normalPool, elitePool, bossPool);
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var result = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 9,
            enteredNight: true,
            isEliteNight: true,
            isBossNight: false,
            transitionToken: "day-9-night-entry");

        result.Choices.Should().HaveCount(3);
        result.Choices.Should().OnlyContain(choice => elitePool.Contains(choice));
        result.Choices.Should().NotContain(choice => normalPool.Contains(choice));
        result.Choices.Should().NotContain(choice => bossPool.Contains(choice));
    }

    // ACC:T18.15
    [Fact]
    [Trait("acceptance", "ACC:T18.15")]
    public void ShouldKeepDayNightProgressionUnblocked_WhenNightlyRewardProcessingRuns()
    {
        var rewardPools = CreateRewardPools();
        var sut = CreateSut(rewardPools, fallbackGold: 200);

        var firstTick = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 10,
            enteredNight: false,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-10-day-loop");
        var secondTick = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 10,
            enteredNight: true,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-10-night-entry");
        var thirdTick = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 11,
            enteredNight: false,
            isEliteNight: false,
            isBossNight: false,
            transitionToken: "day-11-day-loop");
        var fourthTick = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 11,
            enteredNight: true,
            isEliteNight: true,
            isBossNight: false,
            transitionToken: "day-11-night-entry");
        var duplicateNightTick = sut.ProcessPhaseTransition(
            runId: "run-18",
            dayNumber: 11,
            enteredNight: true,
            isEliteNight: true,
            isBossNight: false,
            transitionToken: "day-11-night-entry");

        firstTick.Triggered.Should().BeFalse();
        secondTick.Triggered.Should().BeTrue();
        thirdTick.Triggered.Should().BeFalse();
        fourthTick.Triggered.Should().BeTrue();
        duplicateNightTick.Triggered.Should().BeFalse();
        sut.ProgressionStepCount.Should().Be(5);
        sut.TriggerCount.Should().Be(2);
    }

    private static RewardManager CreateSut(
        IReadOnlyDictionary<NightType, IReadOnlyList<string>> rewardPools,
        int fallbackGold)
    {
        return new RewardManager(
            rewardPools,
            fallbackGold,
            () => DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"));
    }

    private static Dictionary<NightType, IReadOnlyList<string>> CreateRewardPools(
        IReadOnlyList<string>? normal = null,
        IReadOnlyList<string>? elite = null,
        IReadOnlyList<string>? boss = null)
    {
        return new Dictionary<NightType, IReadOnlyList<string>>
        {
            [NightType.Normal] = normal ?? new[] { "normal-a", "normal-b", "normal-c", "normal-d" },
            [NightType.Elite] = elite ?? new[] { "elite-a", "elite-b", "elite-c", "elite-d" },
            [NightType.Boss] = boss ?? new[] { "boss-a", "boss-b", "boss-c", "boss-d" },
        };
    }

    private static IReadOnlyList<string> ExtractChoices(RewardOffered offeredEvent)
    {
        return new[] { offeredEvent.OptionA, offeredEvent.OptionB, offeredEvent.OptionC };
    }

}
