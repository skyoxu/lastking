using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightStateGateTests
{
    // ACC:T5.9
    [Fact]
    public void ShouldEmitNoCadenceDrivenSpawns_WhenGameplayStateIsNotNight()
    {
        var sut = new WaveManager();
        var emissions = new List<StateGatedSpawnEmission>();

        emissions.AddRange(sut.GenerateCadenceDrivenSpawns(NightGameplayState.Day, elapsedSeconds: 20.0, spawnPointId: "North", enemyId: "Grunt"));
        emissions.AddRange(sut.GenerateCadenceDrivenSpawns(NightGameplayState.Dusk, elapsedSeconds: 20.0, spawnPointId: "North", enemyId: "Grunt"));
        emissions.AddRange(sut.GenerateCadenceDrivenSpawns(NightGameplayState.Dawn, elapsedSeconds: 20.0, spawnPointId: "North", enemyId: "Grunt"));

        emissions.Should().BeEmpty();
    }

    [Fact]
    public void ShouldEmitCadenceDrivenSpawns_WhenGameplayStateIsNight()
    {
        var sut = new WaveManager();

        var emissions = sut.GenerateCadenceDrivenSpawns(
            NightGameplayState.Night,
            elapsedSeconds: 20.0,
            spawnPointId: "East",
            enemyId: "Brute");

        emissions.Should().ContainSingle();
        emissions[0].SpawnPointId.Should().Be("East");
        emissions[0].EnemyId.Should().Be("Brute");
        emissions[0].SourceState.Should().Be(NightGameplayState.Night);
    }

    [Fact]
    public void ShouldEmitNoCadenceDrivenSpawns_WhenNightTickIsOffCadence()
    {
        var sut = new WaveManager();

        var emissions = sut.GenerateCadenceDrivenSpawns(
            NightGameplayState.Night,
            elapsedSeconds: 25.0,
            spawnPointId: "South",
            enemyId: "Archer");

        emissions.Should().BeEmpty();
    }

}
