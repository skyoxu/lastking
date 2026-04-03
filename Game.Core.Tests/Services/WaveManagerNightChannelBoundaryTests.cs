using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightChannelBoundaryTests
{
    // ACC:T5.18
    [Fact]
    public void ShouldKeepRegularCadenceInsideNightSpawnWindow_WhenOrchestratingPerChannelEmissions()
    {
        var sut = new WaveManager();
        var request = new NightChannelBoundaryRequest(
            NightDurationSeconds: 120,
            RegularCadenceSeconds: 10,
            RegularWindowStartSeconds: 0,
            RegularWindowEndSeconds: 96,
            BossNightLimit: 2,
            BossSpawnAttempts: 5);

        var result = sut.OrchestrateNightChannels(request);

        result.RegularEmissionTimes.Should().OnlyContain(time =>
            time >= request.RegularWindowStartSeconds &&
            time < request.RegularWindowEndSeconds);
    }

    [Fact]
    public void ShouldRefuseAdditionalBossEmissions_WhenBossNightCountAlreadyReached()
    {
        var sut = new WaveManager();
        var request = new NightChannelBoundaryRequest(
            NightDurationSeconds: 120,
            RegularCadenceSeconds: 10,
            RegularWindowStartSeconds: 0,
            RegularWindowEndSeconds: 96,
            BossNightLimit: 2,
            BossSpawnAttempts: 6);

        var result = sut.OrchestrateNightChannels(request);

        result.BossEmissionCount.Should().Be(request.BossNightLimit);
        result.RejectedBossAttempts.Should().Be(4);
    }

}
