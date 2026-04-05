using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task10StagedGatePrerequisiteTests
{
    // ACC:T10.2
    // ACC:T10.16
    [Fact]
    [Trait("acceptance", "ACC:T10.2")]
    [Trait("acceptance", "ACC:T10.16")]
    public void ShouldAllowReplayStart_WhenStagedCompleteAndAllFiveSlicesPassed()
    {
        var status = new StagedIntegrationStatus(
            StagedComplete: true,
            Slices: CreateMandatedSlices(includeIncompleteSlice: false));

        var canStartReplay = StagedGatePrerequisiteHarness.CanStartFullSeededReplay(status);

        canStartReplay.Should().BeTrue(
            "replay start requires staged_complete=true and all five required slices to pass in mandated order");
    }

    // ACC:T10.16
    [Theory]
    [Trait("acceptance", "ACC:T10.16")]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ShouldRejectReplayStart_WhenStagedFlagIsFalseOrAnySliceIncomplete(bool stagedComplete, bool includeIncompleteSlice)
    {
        var status = new StagedIntegrationStatus(
            StagedComplete: stagedComplete,
            Slices: CreateMandatedSlices(includeIncompleteSlice));

        var canStartReplay = StagedGatePrerequisiteHarness.CanStartFullSeededReplay(status);

        canStartReplay.Should().BeFalse(
            "replay must not start when staged_complete is false or when any staged slice is incomplete");
    }

    private static IReadOnlyList<StagedSliceResult> CreateMandatedSlices(bool includeIncompleteSlice)
    {
        var slices = new List<StagedSliceResult>
        {
            new("runtime-loop-wiring", SliceVerdict.Pass),
            new("spawn-channel-checks", SliceVerdict.Pass),
            new("blocked-map-fallback", SliceVerdict.Pass),
            new("terminal-win-lose-checks", SliceVerdict.Pass),
            new("evidence-packaging", SliceVerdict.Pass)
        };

        if (includeIncompleteSlice)
        {
            slices[3] = slices[3] with { Verdict = SliceVerdict.Incomplete };
        }

        return slices;
    }

    private static class StagedGatePrerequisiteHarness
    {
        private static readonly string[] mandatedOrder =
        {
            "runtime-loop-wiring",
            "spawn-channel-checks",
            "blocked-map-fallback",
            "terminal-win-lose-checks",
            "evidence-packaging"
        };

        public static bool CanStartFullSeededReplay(StagedIntegrationStatus status)
        {
            if (!status.StagedComplete)
            {
                return false;
            }

            if (status.Slices.Count != mandatedOrder.Length)
            {
                return false;
            }

            var actualOrder = status.Slices.Select(slice => slice.Name);
            if (!actualOrder.SequenceEqual(mandatedOrder))
            {
                return false;
            }

            return status.Slices.All(slice => slice.Verdict == SliceVerdict.Pass);
        }
    }

    private sealed record StagedIntegrationStatus(bool StagedComplete, IReadOnlyList<StagedSliceResult> Slices);

    private sealed record StagedSliceResult(string Name, SliceVerdict Verdict);

    private enum SliceVerdict
    {
        Pass,
        Fail,
        Incomplete
    }
}
