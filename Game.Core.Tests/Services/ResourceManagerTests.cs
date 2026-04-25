using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResourceManagerTests
{
    // ACC:T12.3
    // ACC:T12.4
    // ACC:T12.6
    // ACC:T12.20
    // ACC:T12.21
    [Trait("acceptance", "ACC:T12.3")]
    [Trait("acceptance", "ACC:T12.4")]
    [Trait("acceptance", "ACC:T12.6")]
    [Trait("acceptance", "ACC:T12.20")]
    [Trait("acceptance", "ACC:T12.21")]
    // ACC:T44.1
    // ACC:T44.2
    [Fact]
    public void ShouldExposeCanonicalInitialState_WhenConstructed()
    {
        var sut = new ResourceManager();

        sut.Gold.Should().Be(800);
        sut.Iron.Should().Be(150);
        sut.PopulationCap.Should().Be(50);
        sut.GetSnapshot().Should().Be(new ResourceSnapshot(800, 150, 50));
    }

    // ACC:T12.7
    // ACC:T12.14
    // ACC:T12.16
    // ACC:T44.3
    [Fact]
    [Trait("acceptance", "ACC:T12.7")]
    [Trait("acceptance", "ACC:T12.14")]
    [Trait("acceptance", "ACC:T12.16")]
    public void ShouldApplyExactCost_WhenSpendSucceeds()
    {
        var sut = new ResourceManager();

        var result = sut.TrySpend(120, 30, "unit-spend");

        result.Succeeded.Should().BeTrue();
        sut.Gold.Should().Be(680);
        sut.Iron.Should().Be(120);
        sut.PopulationCap.Should().Be(50);
    }

    // ACC:T12.8
    // ACC:T12.10
    // ACC:T12.15
    [Fact]
    [Trait("acceptance", "ACC:T12.8")]
    [Trait("acceptance", "ACC:T12.10")]
    [Trait("acceptance", "ACC:T12.15")]
    public void ShouldRejectInvalidSpendAtomically_WhenInputIsNegativeOrInsufficient()
    {
        var sut = new ResourceManager();
        var baseline = sut.GetSnapshot();

        var negative = sut.TrySpend(-1, 1, "negative");
        var insufficient = sut.TrySpend(9999, 1, "insufficient");

        negative.Succeeded.Should().BeFalse();
        insufficient.Succeeded.Should().BeFalse();
        sut.GetSnapshot().Should().Be(baseline);
    }

    // ACC:T12.10
    // ACC:T12.15
    [Fact]
    [Trait("acceptance", "ACC:T12.10")]
    [Trait("acceptance", "ACC:T12.15")]
    public void ShouldRejectNegativeAddAndSubtract_WhenInputIsNegative()
    {
        var sut = new ResourceManager();
        var baseline = sut.GetSnapshot();

        var negativeAdd = sut.TryAdd(-1, 0, 0, "negative-add");
        var negativeSubtract = sut.TrySubtract(-1, 0, 0, "negative-subtract");

        negativeAdd.Succeeded.Should().BeFalse();
        negativeSubtract.Succeeded.Should().BeFalse();
        sut.GetSnapshot().Should().Be(baseline);
    }

    // ACC:T12.9
    [Fact]
    [Trait("acceptance", "ACC:T12.9")]
    public void ShouldTreatZeroDeltaAsDeterministicNoOp_WhenMutatingResources()
    {
        var sut = new ResourceManager();
        var baseline = sut.GetSnapshot();

        var add = sut.TryAdd(0, 0, 0, "zero-add");
        var subtract = sut.TrySubtract(0, 0, 0, "zero-subtract");
        var spend = sut.TrySpend(0, 0, "zero-spend");

        add.Succeeded.Should().BeTrue();
        subtract.Succeeded.Should().BeTrue();
        spend.Succeeded.Should().BeTrue();
        sut.GetSnapshot().Should().Be(baseline);
    }

    // ACC:T12.11
    // ACC:T12.12
    // ACC:T12.13
    // ACC:T12.18
    // ACC:T12.19
    [Trait("acceptance", "ACC:T12.11")]
    [Trait("acceptance", "ACC:T12.12")]
    [Trait("acceptance", "ACC:T12.13")]
    [Trait("acceptance", "ACC:T12.18")]
    [Trait("acceptance", "ACC:T12.19")]
    [Fact]
    public void ShouldRejectOverflowOrUnderflow_WhenApplyingMutation()
    {
        var sut = new ResourceManager();
        var baseline = sut.GetSnapshot();

        var overflow = sut.TryAdd(int.MaxValue, int.MaxValue, int.MaxValue, "overflow");
        var underflow = sut.TrySubtract(900, 200, 100, "underflow");

        overflow.Succeeded.Should().BeFalse();
        underflow.Succeeded.Should().BeFalse();
        sut.GetSnapshot().Should().Be(baseline);
    }

    // ACC:T12.5
    // ACC:T12.17
    [Fact]
    [Trait("acceptance", "ACC:T12.5")]
    [Trait("acceptance", "ACC:T12.17")]
    public void ShouldPreserveDeterministicSnapshotRoundtrip_WhenExportingAndImportingState()
    {
        var seed = new ResourceManager();
        seed.TryAdd(200, 10, 5, "seed").Succeeded.Should().BeTrue();

        var snapshotJson = seed.ExportSnapshot();
        var recovered = new ResourceManager();
        var import = recovered.TryImportSnapshot(snapshotJson);

        import.Accepted.Should().BeTrue();
        recovered.GetSnapshot().Should().Be(seed.GetSnapshot());
        recovered.ExportSnapshot().Should().Be(snapshotJson);
    }
}
