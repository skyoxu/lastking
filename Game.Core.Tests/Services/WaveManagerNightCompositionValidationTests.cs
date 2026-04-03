using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightCompositionValidationTests
{
    // ACC:T5.16
    [Fact]
    public void ShouldRejectNightRun_WhenCadenceFieldsAreMissing()
    {
        var sut = new WaveManager();
        var composition = CreateValidComposition() with
        {
            CadenceSeconds = null,
            CadenceWindowSeconds = null
        };

        var result = sut.TryStartNightSpawn(composition);

        result.ValidationPassed.Should().BeFalse();
        result.Started.Should().BeFalse();
        result.Errors.Should().Contain("cadence_seconds_required");
        result.Errors.Should().Contain("cadence_window_seconds_required");
    }

    // ACC:T5.16
    [Fact]
    public void ShouldRejectNightRun_WhenDeterministicSequencingKeysAreMissing()
    {
        var sut = new WaveManager();
        var composition = CreateValidComposition() with
        {
            DeterministicSequencingKeys = Array.Empty<string>()
        };

        var result = sut.TryStartNightSpawn(composition);

        result.ValidationPassed.Should().BeFalse();
        result.Started.Should().BeFalse();
        result.Errors.Should().Contain("deterministic_sequencing_keys_required");
    }

    // ACC:T5.16
    [Fact]
    public void ShouldNotStartNightSpawnFlow_WhenCompositionValidationFails()
    {
        var sut = new WaveManager();
        var composition = CreateValidComposition() with
        {
            DeterministicTieBreakerKey = null
        };

        var result = sut.TryStartNightSpawn(composition);

        result.ValidationPassed.Should().BeFalse();
        result.Started.Should().BeFalse();
        result.SpawnEvents.Should().Equal("validate");
    }

    [Fact]
    public void ShouldStartNightSpawnFlow_WhenCompositionContractIsValid()
    {
        var sut = new WaveManager();
        var composition = CreateValidComposition();

        var result = sut.TryStartNightSpawn(composition);

        result.ValidationPassed.Should().BeTrue();
        result.Started.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.SpawnEvents.Should().Equal("validate", "start", "emit:wave_index", "emit:spawn_point_id");
    }

    private static NightSpawnCompositionContract CreateValidComposition()
    {
        return new NightSpawnCompositionContract(
            CadenceSeconds: 10,
            CadenceWindowSeconds: 96,
            DeterministicRngKey: "pcg32",
            DeterministicTieBreakerKey: "seeded_pseudo_random",
            DeterministicSequencingKeys: new[] { "wave_index", "spawn_point_id" });
    }
}
