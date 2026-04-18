using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class AchievementSyncDeterminismTests
{
    // ACC:T27.14
    [Fact]
    public void ShouldProduceIdenticalUnlockEvidence_WhenInputIsIdentical()
    {
        var unlocks = new[]
        {
            new UnlockInput("first-blood", "player-1"),
            new UnlockInput("treasure-hunter", "player-1")
        };

        var sut = new AchievementSyncEvidenceService();

        var firstRun = sut.BuildUnlockEvidence(unlocks);
        var secondRun = sut.BuildUnlockEvidence(unlocks);

        firstRun.Should().OnlyContain(record =>
            !string.IsNullOrWhiteSpace(record.Timestamp) &&
            !string.IsNullOrWhiteSpace(record.SequenceAnchor));

        secondRun.Should().OnlyContain(record =>
            !string.IsNullOrWhiteSpace(record.Timestamp) &&
            !string.IsNullOrWhiteSpace(record.SequenceAnchor));

        secondRun.Should().BeEquivalentTo(firstRun, options => options.WithStrictOrdering());
    }

    // ACC:T27.15
    [Fact]
    public void ShouldFailGate_WhenUnlockRecordMissesEvidenceOrDivergesFromBaseline()
    {
        var unlocks = new[]
        {
            new UnlockInput("first-blood", "player-1"),
            new UnlockInput("treasure-hunter", "player-1")
        };

        var syncService = new AchievementSyncEvidenceService();
        var baseline = syncService.BuildUnlockEvidence(unlocks);
        var candidate = syncService.BuildUnlockEvidence(unlocks).ToList();

        candidate[0] = candidate[0] with { SequenceAnchor = string.Empty };

        var gate = new AchievementDeterminismGate();

        var gateResult = gate.Passes(candidate, baseline);

        gateResult.Should().BeFalse("records missing timestamp or sequence evidence, or diverging from baseline, must be rejected");
    }

}
