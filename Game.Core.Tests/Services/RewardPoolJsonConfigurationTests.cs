using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services.Reward;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RewardPoolJsonConfigurationTests
{
    // ACC:T18.6
    [Fact]
    [Trait("acceptance", "ACC:T18.6")]
    public void ShouldUseUpdatedNormalPoolEntries_WhenJsonConfigurationChangesBetweenNightTriggers()
    {
        var jsonV1 = BuildJson(
            fallbackGold: 200,
            normalPool: new[] { "normal-v1-a", "normal-v1-b", "normal-v1-c", "normal-v1-d" },
            elitePool: new[] { "elite-a", "elite-b", "elite-c" },
            bossPool: new[] { "boss-a", "boss-b", "boss-c" });
        var jsonV2 = BuildJson(
            fallbackGold: 200,
            normalPool: new[] { "normal-v2-a", "normal-v2-b", "normal-v2-c", "normal-v2-d" },
            elitePool: new[] { "elite-a", "elite-b", "elite-c" },
            bossPool: new[] { "boss-a", "boss-b", "boss-c" });

        var sut = CreateSut(jsonV1);

        var first = sut.TriggerNight(
            runId: "run-18",
            dayNumber: 1,
            isEliteNight: false,
            isBossNight: false);

        sut.ReloadJson(jsonV2);

        var second = sut.TriggerNight(
            runId: "run-18",
            dayNumber: 2,
            isEliteNight: false,
            isBossNight: false);

        first.Choices.Should().Equal("normal-v1-a", "normal-v1-b", "normal-v1-c");
        second.Choices.Should().Equal("normal-v2-a", "normal-v2-b", "normal-v2-c");
        second.Choices.Should().OnlyContain(choice => choice.StartsWith("normal-v2-", StringComparison.Ordinal));
    }

    // ACC:T18.16
    [Fact]
    [Trait("acceptance", "ACC:T18.16")]
    public void ShouldGenerateThreeChoicesAndNoFallback_WhenActivePoolHasEnoughEntries()
    {
        var json = BuildJson(
            fallbackGold: 200,
            normalPool: new[] { "normal-a", "normal-b", "normal-c" },
            elitePool: new[] { "elite-a", "elite-b", "elite-c", "elite-d" },
            bossPool: new[] { "boss-a", "boss-b", "boss-c" });

        var sut = CreateSut(json);

        var result = sut.TriggerNight(
            runId: "run-18",
            dayNumber: 3,
            isEliteNight: true,
            isBossNight: false);

        result.ActiveNightType.Should().Be(NightType.Elite);
        result.Choices.Should().Equal("elite-a", "elite-b", "elite-c");
        result.GrantedFallbackGold.Should().Be(0);
        result.OfferedEvent.Should().NotBeNull();

        var offeredEvent = result.OfferedEvent!;
        new[] { offeredEvent.OptionA, offeredEvent.OptionB, offeredEvent.OptionC }
            .Should()
            .Equal(result.Choices);
    }

    [Fact]
    public void ShouldGrantFallbackAndEmitNoOffer_WhenActivePoolIsExhausted()
    {
        var json = BuildJson(
            fallbackGold: 200,
            normalPool: new[] { "normal-a", "normal-b", "normal-c" },
            elitePool: new[] { "elite-a", "elite-b", "elite-c" },
            bossPool: new[] { "boss-a", "boss-b" });

        var sut = CreateSut(json);

        var result = sut.TriggerNight(
            runId: "run-18",
            dayNumber: 4,
            isEliteNight: false,
            isBossNight: true);

        result.ActiveNightType.Should().Be(NightType.Boss);
        result.Choices.Should().BeEmpty();
        result.OfferedEvent.Should().BeNull();
        result.GrantedFallbackGold.Should().Be(200);
    }

    // ACC:T18.9
    [Fact]
    [Trait("acceptance", "ACC:T18.9")]
    public void ShouldApplyUpdatedFallbackGoldAmount_WhenJsonFallbackValueChanges()
    {
        var jsonBefore = BuildJson(
            fallbackGold: 200,
            normalPool: new[] { "normal-a", "normal-b", "normal-c" },
            elitePool: new[] { "elite-a", "elite-b", "elite-c" },
            bossPool: Array.Empty<string>());
        var jsonAfter = BuildJson(
            fallbackGold: 550,
            normalPool: new[] { "normal-a", "normal-b", "normal-c" },
            elitePool: new[] { "elite-a", "elite-b", "elite-c" },
            bossPool: Array.Empty<string>());

        var sut = CreateSut(jsonBefore);

        var first = sut.TriggerNight(
            runId: "run-18",
            dayNumber: 5,
            isEliteNight: false,
            isBossNight: true);

        sut.ReloadJson(jsonAfter);

        var second = sut.TriggerNight(
            runId: "run-18",
            dayNumber: 6,
            isEliteNight: false,
            isBossNight: true);

        first.GrantedFallbackGold.Should().Be(200);
        second.GrantedFallbackGold.Should().Be(550);
    }

    private static RewardPoolJsonRuntime CreateSut(string json)
    {
        return new RewardPoolJsonRuntime(
            json,
            () => DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"));
    }

    private static string BuildJson(
        int fallbackGold,
        IReadOnlyList<string> normalPool,
        IReadOnlyList<string> elitePool,
        IReadOnlyList<string> bossPool)
    {
        return "{"
            + "\"reward\":{"
            + "\"fallback_gold\":" + fallbackGold + ","
            + "\"pools\":{"
            + "\"normal\":" + ToJsonArray(normalPool) + ","
            + "\"elite\":" + ToJsonArray(elitePool) + ","
            + "\"boss\":" + ToJsonArray(bossPool)
            + "}"
            + "}"
            + "}";
    }

    private static string ToJsonArray(IReadOnlyList<string> values)
    {
        return "[" + string.Join(",", values.Select(value => "\"" + value + "\"")) + "]";
    }

}
