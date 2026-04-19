using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class SpawnWaveTimelineDeterminismTests
{
    // ACC:T34.3
    [Fact]
    [Trait("acceptance", "ACC:T34.3")]
    public void ShouldChangeSpawnSequence_WhenSpawnPointOrderChangesButIdsAreSame()
    {
        var sut = new WaveManager();
        var canonicalSpawnPoints = new[] { "Spawn-A", "Spawn-B", "Spawn-C" };
        var reorderedSpawnPoints = new[] { "Spawn-C", "Spawn-A", "Spawn-B" };

        var canonicalTimeline = sut.GenerateRegularSpawns(120.0, isBossNight: false, canonicalSpawnPoints);
        var reorderedTimeline = sut.GenerateRegularSpawns(120.0, isBossNight: false, reorderedSpawnPoints);

        reorderedTimeline
            .Select(emission => emission.ElapsedSeconds)
            .Should()
            .Equal(canonicalTimeline.Select(emission => emission.ElapsedSeconds));

        reorderedTimeline
            .Select(emission => emission.SpawnPointId)
            .Should()
            .NotEqual(canonicalTimeline.Select(emission => emission.SpawnPointId));
    }

    [Fact]
    public void ShouldKeepTimelineDeterministic_WhenInputsAreIdentical()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A", "Spawn-B" };

        var firstRun = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);
        var secondRun = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);

        secondRun.Select(emission => emission.ElapsedSeconds).Should().Equal(firstRun.Select(emission => emission.ElapsedSeconds));
        secondRun.Select(emission => emission.SpawnPointId).Should().Equal(firstRun.Select(emission => emission.SpawnPointId));
    }

    [Fact]
    public void ShouldNotEmitInInactiveWindow_WhenGeneratingRegularSpawns()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A", "Spawn-B" };

        var emissions = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints, cadenceSeconds: 10.0);

        emissions.Should().NotBeEmpty();
        emissions.Should().OnlyContain(emission => emission.ElapsedSeconds < 96.0);
    }
}
