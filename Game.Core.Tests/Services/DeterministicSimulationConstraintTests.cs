using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class DeterministicSimulationConstraintTests
{
    private const string SchemaRejectedReason = "CFG_SCHEMA_REJECTED";

    // ACC:T35.2
    [Fact]
    public void ShouldPreserveLastKnownGoodSnapshot_WhenInvalidRevisionIsReloaded()
    {
        var manager = new ConfigManager();
        var baselineValidRevision = BuildValidRuntimeConfigJson(daySeconds: 240, nightSeconds: 120, day1Budget: 50, dailyGrowth: 1.2m);
        var initial = manager.LoadInitialFromJson(baselineValidRevision, "res://Config/revision-valid.json");

        var baselineInvalidRevision = BuildPressureNormalizationRevisionJson(baseline: 10.0, minPressure: 40.0, maxPressure: 10.0);
        var firstRejected = manager.ReloadFromJson(baselineInvalidRevision, "res://Config/revision-invalid-a.json");
        var secondRejected = manager.ReloadFromJson(baselineInvalidRevision, "res://Config/revision-invalid-a.json");

        initial.Accepted.Should().BeTrue();
        firstRejected.Accepted.Should().BeFalse();
        secondRejected.Accepted.Should().BeFalse();
        firstRejected.Snapshot.Should().Be(initial.Snapshot);
        secondRejected.Snapshot.Should().Be(initial.Snapshot);
        firstRejected.ReasonCodes.Should().NotBeEmpty();
        firstRejected.ReasonCodes.Should().Equal(secondRejected.ReasonCodes);
    }

    // ACC:T35.18
    [Fact]
    public void ShouldEmitSchemaRejectedReason_WhenPressureBaselineViolatesLowerBound()
    {
        var manager = new ConfigManager();
        var baselineValidRevision = BuildValidRuntimeConfigJson(daySeconds: 240, nightSeconds: 120, day1Budget: 50, dailyGrowth: 1.2m);
        manager.LoadInitialFromJson(baselineValidRevision, "res://Config/revision-valid.json").Accepted.Should().BeTrue();

        var belowLowerBoundRevision = BuildPressureNormalizationRevisionJson(baseline: -1.0, minPressure: 0.0, maxPressure: 1.0);
        var rejected = manager.ReloadFromJson(belowLowerBoundRevision, "res://Config/revision-invalid-below-bound.json");

        rejected.Accepted.Should().BeFalse();
        rejected.ReasonCodes.Should().Contain(SchemaRejectedReason);
    }

    [Fact]
    public void ShouldRemainDeterministicForReasonCodes_WhenReplayingInvalidRevisionFixtures()
    {
        var manager = new ConfigManager();
        var baselineValidRevision = BuildValidRuntimeConfigJson(daySeconds: 300, nightSeconds: 150, day1Budget: 60, dailyGrowth: 1.3m);
        manager.LoadInitialFromJson(baselineValidRevision, "res://Config/revision-valid-v2.json").Accepted.Should().BeTrue();

        var baselineInvalidRevisions = new[]
        {
            BuildPressureNormalizationRevisionJson(baseline: -1.0, minPressure: 0.0, maxPressure: 1.0),
            BuildPressureNormalizationRevisionJson(baseline: 10.0, minPressure: 50.0, maxPressure: 10.0),
        };

        var firstPass = EvaluateRejectionReasons(manager, baselineInvalidRevisions);
        var secondPass = EvaluateRejectionReasons(manager, baselineInvalidRevisions);

        firstPass.Should().HaveCount(2);
        firstPass.Should().OnlyContain(reason => !string.Equals(reason, "ACCEPTED", System.StringComparison.Ordinal));
        firstPass.Should().Equal(secondPass);
    }

    private static IReadOnlyList<string> EvaluateRejectionReasons(ConfigManager manager, IReadOnlyList<string> revisions)
    {
        var reasonSnapshots = new List<string>(revisions.Count);
        for (var index = 0; index < revisions.Count; index++)
        {
            var result = manager.ReloadFromJson(revisions[index], $"res://Config/revision-invalid-{index}.json");
            var reasonKey = result.Accepted
                ? "ACCEPTED"
                : string.Join("|", result.ReasonCodes.OrderBy(code => code, System.StringComparer.Ordinal));
            reasonSnapshots.Add(reasonKey);
        }

        return reasonSnapshots;
    }

    private static string BuildValidRuntimeConfigJson(int daySeconds, int nightSeconds, int day1Budget, decimal dailyGrowth)
    {
        var daySecondsToken = daySeconds.ToString(CultureInfo.InvariantCulture);
        var nightSecondsToken = nightSeconds.ToString(CultureInfo.InvariantCulture);
        var day1BudgetToken = day1Budget.ToString(CultureInfo.InvariantCulture);
        var dailyGrowthToken = dailyGrowth.ToString(CultureInfo.InvariantCulture);

        return $$"""
        {
          "time": { "day_seconds": {{daySecondsToken}}, "night_seconds": {{nightSecondsToken}} },
          "waves": { "normal": { "day1_budget": {{day1BudgetToken}}, "daily_growth": {{dailyGrowthToken}} } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": { "cadence_seconds": 10 },
          "boss": { "count": 2 }
        }
        """;
    }

    private static string BuildPressureNormalizationRevisionJson(double baseline, double minPressure, double maxPressure)
    {
        var baselineToken = baseline.ToString("R", CultureInfo.InvariantCulture);
        var minPressureToken = minPressure.ToString("R", CultureInfo.InvariantCulture);
        var maxPressureToken = maxPressure.ToString("R", CultureInfo.InvariantCulture);

        return $$"""
        {
          "baseline": {{baselineToken}},
          "min_pressure": {{minPressureToken}},
          "max_pressure": {{maxPressureToken}},
          "normalization_factors": [1.0, 0.9, 1.1],
          "constraints": { "range_check": true }
        }
        """;
    }
}
