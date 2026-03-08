using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveBudgetFromConfigTests
{
    // ACC:T2.9
    [Fact]
    public void ShouldUseConfigValues_WhenResolvingRuntimeWaveBudget()
    {
        var config = new Dictionary<string, string>
        {
            ["day1"] = "50",
            ["dailyGrowth"] = "1.2"
        };

        var budget = ResolveFromConfig(config);

        budget.Day1.Should().Be(50);
        budget.DailyGrowth.Should().Be(1.2m);
    }

    // ACC:T2.9
    [Fact]
    public void ShouldKeepPreviousBudgetUnchanged_WhenConfigIsInvalid()
    {
        var previous = new WaveBudget(50, 1.2m);
        var config = new Dictionary<string, string>
        {
            ["day1"] = "-10",
            ["dailyGrowth"] = "abc"
        };

        var success = TryResolveFromConfig(config, previous, out var runtimeBudget);

        success.Should().BeFalse();
        runtimeBudget.Should().Be(previous);
    }

    private static WaveBudget ResolveFromConfig(IReadOnlyDictionary<string, string> config)
    {
        var day1 = int.Parse(config["day1"], CultureInfo.InvariantCulture);
        var growth = decimal.Parse(config["dailyGrowth"], CultureInfo.InvariantCulture);
        return new WaveBudget(day1, growth);
    }

    private static bool TryResolveFromConfig(
        IReadOnlyDictionary<string, string> config,
        WaveBudget previous,
        out WaveBudget runtimeBudget)
    {
        runtimeBudget = previous;
        if (!config.TryGetValue("day1", out var day1Raw) || !int.TryParse(day1Raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day1))
        {
            return false;
        }

        if (!config.TryGetValue("dailyGrowth", out var growthRaw) || !decimal.TryParse(growthRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var growth))
        {
            return false;
        }

        if (day1 < 0 || growth <= 0m)
        {
            return false;
        }

        runtimeBudget = new WaveBudget(day1, growth);
        return true;
    }

    private sealed record WaveBudget(int Day1, decimal DailyGrowth);
}
