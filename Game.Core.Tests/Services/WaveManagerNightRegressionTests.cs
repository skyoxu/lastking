using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightRegressionTests
{
    // ACC:T5.20
    [Fact]
    public void ShouldProduceDeterministicTrace_WhenSeedAndInputsAreIdentical()
    {
        var sut = new WaveManager();
        var entries = new[]
        {
            new NightTraceEntry("Slime", 2, 1),
            new NightTraceEntry("Bat", 1, 2)
        };

        var firstRun = sut.TryRunNightTrace(seed: 712341, entries, remainingBudget: 20);
        var secondRun = sut.TryRunNightTrace(seed: 712341, entries, remainingBudget: 20);

        firstRun.Accepted.Should().BeTrue();
        secondRun.Accepted.Should().BeTrue();
        secondRun.Trace.Should().Equal(firstRun.Trace);
    }

    [Fact]
    public void ShouldRefuseRunAndKeepStateUnchanged_WhenCompositionIsMalformed()
    {
        var sut = new WaveManager();
        var malformedEntries = new[]
        {
            new NightTraceEntry("Slime", 2, 0)
        };

        var result = sut.TryRunNightTrace(seed: 712341, malformedEntries, remainingBudget: 5);

        result.Accepted.Should().BeFalse();
        result.Trace.Should().BeEmpty();
        result.RemainingBudget.Should().Be(5);
    }

    [Fact]
    public void ShouldMatchBaselineTrace_WhenSeedAndInputsAreUnchanged()
    {
        var sut = new WaveManager();
        var entries = new[]
        {
            new NightTraceEntry("Imp", 3, 1),
            new NightTraceEntry("Wolf", 2, 2)
        };

        var baselineTrace = sut.TryRunNightTrace(seed: 91277, entries, remainingBudget: 20).Trace;
        var replayTrace = sut.TryRunNightTrace(seed: 91277, entries, remainingBudget: 20).Trace;

        replayTrace.Should().Equal(
            baselineTrace,
            "identical seed and inputs must never drift from the baseline trace");
    }
}
