using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightDeterminismTests
{
    // ACC:T5.17
    [Fact]
    public void ShouldReplayIdenticalEmissionOrderAndCadence_WhenInputsAndSeedAreRepeated()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "North", "East", "South" };
        var compositionPayload = new[] { "Grunt", "Archer", "Brute" };
        var runConfiguration = new NightRunConfiguration(CadenceSeconds: 10.0, NightDurationSeconds: 45.0);

        var firstRun = sut.GenerateDeterministicNightSpawns(runConfiguration, spawnPoints, compositionPayload, deterministicSeed: 77);
        var secondRun = sut.GenerateDeterministicNightSpawns(runConfiguration, spawnPoints, compositionPayload, deterministicSeed: 77);

        BuildScheduleSnapshot(secondRun).Should().Be(BuildScheduleSnapshot(firstRun));
    }

    // ACC:T5.17
    [Fact]
    public void ShouldReplayIdenticalEmissionOrderAndCadence_WhenManagerInstancesAreFreshWithSameInputs()
    {
        var firstSut = new WaveManager();
        var secondSut = new WaveManager();
        var spawnPoints = new[] { "North", "East", "South" };
        var compositionPayload = new[] { "Grunt", "Archer", "Brute" };
        var runConfiguration = new NightRunConfiguration(CadenceSeconds: 10.0, NightDurationSeconds: 45.0);

        var firstRun = firstSut.GenerateDeterministicNightSpawns(runConfiguration, spawnPoints, compositionPayload, deterministicSeed: 77);
        var secondRun = secondSut.GenerateDeterministicNightSpawns(runConfiguration, spawnPoints, compositionPayload, deterministicSeed: 77);

        BuildScheduleSnapshot(secondRun).Should().Be(BuildScheduleSnapshot(firstRun));
    }

    [Fact]
    public void ShouldEmitDifferentOrderOrCadence_WhenDeterministicSeedChanges()
    {
        var firstSut = new WaveManager();
        var secondSut = new WaveManager();
        var spawnPoints = new[] { "North", "East", "South" };
        var compositionPayload = new[] { "Grunt", "Archer", "Brute" };
        var runConfiguration = new NightRunConfiguration(CadenceSeconds: 10.0, NightDurationSeconds: 45.0);

        var firstRun = firstSut.GenerateDeterministicNightSpawns(runConfiguration, spawnPoints, compositionPayload, deterministicSeed: 77);
        var secondRun = secondSut.GenerateDeterministicNightSpawns(runConfiguration, spawnPoints, compositionPayload, deterministicSeed: 78);

        BuildScheduleSnapshot(secondRun).Should().NotBe(BuildScheduleSnapshot(firstRun));
    }

    [Fact]
    public void ShouldRefuseNightSpawnEmission_WhenSpawnPointsOrCompositionPayloadAreMissing()
    {
        var sut = new WaveManager();
        var runConfiguration = new NightRunConfiguration(CadenceSeconds: 10.0, NightDurationSeconds: 45.0);

        var noSpawnPoints = sut.GenerateDeterministicNightSpawns(runConfiguration, Array.Empty<string>(), new[] { "Grunt" }, deterministicSeed: 77);
        var noComposition = sut.GenerateDeterministicNightSpawns(runConfiguration, new[] { "North" }, Array.Empty<string>(), deterministicSeed: 77);

        noSpawnPoints.Should().BeEmpty();
        noComposition.Should().BeEmpty();
    }

    private static string BuildScheduleSnapshot(IReadOnlyList<DeterministicNightSpawnEmission> emissions)
    {
        return string.Join(
            "|",
            emissions.Select((emission, index) =>
                $"{index}:{emission.ElapsedSeconds:0.###}:{emission.SpawnPointId}:{emission.EnemyId}"));
    }
}
