using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BattleReportOutcomeStabilityTests
{
    // ACC:T39.3
    [Fact]
    public void ShouldKeepMatchOutcomeStableExceptConfigMetadata_WhenBattleInputsAreIdentical()
    {
        var simulationService = new DeterministicSimulationLoopService();
        var factory = new BattleReportFactory();
        var initialEnemyPositions = new[] { 3, 8, 13, 21 };
        var targetThreatPriority = new[] { 2, 10, 4, 1 };
        const uint seed = 20260421u;
        const int ticks = 12;

        var baselineTrace = simulationService.RunLoop(initialEnemyPositions, targetThreatPriority, seed, ticks);
        var replayTrace = simulationService.RunLoop(initialEnemyPositions, targetThreatPriority, seed, ticks);

        var baselineReport = factory.CreateReport(baselineTrace, "cfg-hash-a", "config_hash");
        var replayReport = factory.CreateReport(replayTrace, "cfg-hash-b", "config_hash");

        replayReport.MatchResult.Should().BeEquivalentTo(baselineReport.MatchResult);
        replayReport.Metadata.Should().ContainKey("config_hash");
        replayReport.Metadata["config_hash"].Should().Be("cfg-hash-b");
        baselineReport.Metadata["config_hash"].Should().Be("cfg-hash-a");
    }

    // ACC:T39.3
    [Fact]
    public void ShouldChangeMatchResultFingerprint_WhenBattleInputsDiffer()
    {
        var simulationService = new DeterministicSimulationLoopService();
        var factory = new BattleReportFactory();
        var targetThreatPriority = new[] { 2, 10, 4, 1 };
        const uint seed = 20260421u;
        const int ticks = 12;

        var baselineTrace = simulationService.RunLoop(new[] { 3, 8, 13, 21 }, targetThreatPriority, seed, ticks);
        var changedTrace = simulationService.RunLoop(new[] { 4, 8, 13, 21 }, targetThreatPriority, seed, ticks);

        var baselineReport = factory.CreateReport(baselineTrace, "cfg-hash-a", "config_hash");
        var changedReport = factory.CreateReport(changedTrace, "cfg-hash-a", "config_hash");

        changedReport.MatchResult.Should().NotBeEquivalentTo(baselineReport.MatchResult);
    }
}
