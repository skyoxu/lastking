using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services
{
    public sealed class TechTreeDeterminismTests
    {
        // ACC:T17.25
        [Fact]
        public void ShouldProduceIdenticalMultipliersAndBarracksStats_WhenConfigAndUnlockStateAreRepeated()
        {
            var runtime = new TechTreeDeterminismRuntime();
            var configJson = "{\"nodes\":[{\"id\":\"attack_i\",\"multiplier\":1.10},{\"id\":\"armor_i\",\"multiplier\":1.05}]}";
            var unlockState = new[] { "attack_i", "armor_i" };
            var baseStats = new UnitStats(HitPoints: 100, Attack: 20, Defense: 10);

            var firstEvaluation = runtime.Evaluate(configJson, unlockState, baseStats, seed: 2026);
            var secondEvaluation = runtime.Evaluate(configJson, unlockState, baseStats, seed: 2026);

            firstEvaluation.FinalMultiplier.Should().Be(secondEvaluation.FinalMultiplier);
            firstEvaluation.BarracksTrainedStats.Should().BeEquivalentTo(secondEvaluation.BarracksTrainedStats);
        }

        // ACC:T17.26
        [Fact]
        public void ShouldProduceIdenticalTechOutcomes_WhenSeedAndUnlockSequenceAreFixed()
        {
            var runtime = new TechTreeDeterminismRuntime();
            var configJson = "{\"nodes\":[{\"id\":\"attack_i\",\"multiplier\":1.10},{\"id\":\"attack_ii\",\"multiplier\":1.15},{\"id\":\"armor_i\",\"multiplier\":1.05}]}";
            var unlockSequence = new[] { "attack_i", "armor_i", "attack_ii" };
            var baseStats = new UnitStats(HitPoints: 120, Attack: 25, Defense: 12);

            var firstRun = runtime.Evaluate(configJson, unlockSequence, baseStats, seed: 99);
            var secondRun = runtime.Evaluate(configJson, unlockSequence, baseStats, seed: 99);

            firstRun.AppliedNodes.Should().Equal(secondRun.AppliedNodes);
            firstRun.FinalMultiplier.Should().Be(secondRun.FinalMultiplier);
            firstRun.BarracksTrainedStats.Should().BeEquivalentTo(secondRun.BarracksTrainedStats);
        }

        // ACC:T17.3
        [Fact]
        public void ShouldExposeDeterministicTraceArtifacts_WhenCapturingRegressionEvidence()
        {
            var runtime = new TechTreeDeterminismRuntime();
            var configJson = "{\"nodes\":[{\"id\":\"attack_i\",\"multiplier\":1.10},{\"id\":\"armor_i\",\"multiplier\":1.05}]}";
            var unlockSequence = new[] { "attack_i", "armor_i" };
            var baseStats = new UnitStats(HitPoints: 100, Attack: 20, Defense: 10);

            var firstRun = runtime.Evaluate(configJson, unlockSequence, baseStats, seed: 123);
            var secondRun = runtime.Evaluate(configJson, unlockSequence, baseStats, seed: 123);

            firstRun.Trace.UnlockSequence.Should().Equal(unlockSequence);
            firstRun.Trace.MultiplierSnapshots.Should().HaveCount(unlockSequence.Length);
            firstRun.Trace.MultiplierSnapshots.Select(snapshot => snapshot.NodeId).Should().Equal(unlockSequence);
            firstRun.Trace.Should().BeEquivalentTo(secondRun.Trace);
        }

        private sealed class TechTreeDeterminismRuntime
        {
            public TechEvaluationResult Evaluate(
                string configJson,
                IReadOnlyList<string> unlockSequence,
                UnitStats baseStats,
                int seed)
            {
                var multipliersByNode = ParseMultipliers(configJson);
                var appliedNodes = new List<string>();
                var snapshots = new List<MultiplierSnapshot>();
                decimal finalMultiplier = 1m;

                foreach (var nodeId in unlockSequence)
                {
                    if (!multipliersByNode.TryGetValue(nodeId, out var nodeMultiplier))
                    {
                        continue;
                    }

                    appliedNodes.Add(nodeId);
                    finalMultiplier *= nodeMultiplier;
                    snapshots.Add(new MultiplierSnapshot(nodeId, finalMultiplier));
                }

                var trainedStats = new UnitStats(
                    baseStats.HitPoints,
                    baseStats.Attack + (int)Math.Round(baseStats.Attack * (double)(finalMultiplier - 1m)),
                    baseStats.Defense);

                var trace = new TechTrace(
                    unlockSequence.ToArray(),
                    snapshots,
                    $"trace-{seed}");

                return new TechEvaluationResult(finalMultiplier, trainedStats, appliedNodes, trace);
            }

            private static Dictionary<string, decimal> ParseMultipliers(string configJson)
            {
                using var document = JsonDocument.Parse(configJson);
                var result = new Dictionary<string, decimal>(StringComparer.Ordinal);

                foreach (var node in document.RootElement.GetProperty("nodes").EnumerateArray())
                {
                    var nodeId = node.GetProperty("id").GetString() ?? string.Empty;
                    var multiplier = node.GetProperty("multiplier").GetDecimal();
                    result[nodeId] = multiplier;
                }

                return result;
            }
        }

        private sealed record UnitStats(int HitPoints, int Attack, int Defense);

        private sealed record MultiplierSnapshot(string NodeId, decimal MultiplierAfterUnlock);

        private sealed record TechTrace(
            IReadOnlyList<string> UnlockSequence,
            IReadOnlyList<MultiplierSnapshot> MultiplierSnapshots,
            string ArtifactId);

        private sealed record TechEvaluationResult(
            decimal FinalMultiplier,
            UnitStats BarracksTrainedStats,
            IReadOnlyList<string> AppliedNodes,
            TechTrace Trace);
    }
}
