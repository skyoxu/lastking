using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightDiagnosticsTests
{
    // ACC:T5.19
    [Fact]
    public void ShouldFailClosedAndEmitStructuredDiagnostics_WhenSpawnCompositionIsMalformed()
    {
        var waveManager = new WaveManager();
        var malformedComposition = new NightSpawnComposition(EnemyArchetype: string.Empty, Count: -2);

        var result = waveManager.ProcessNightSpawn(malformedComposition);

        result.Emissions.Should().BeEmpty("malformed compositions must fail closed without undefined spawn emissions");
        result.Diagnostic.Should().NotBeNull();
        result.Diagnostic!.Reason.Should().Be("Malformed spawn composition rejected");
        result.Diagnostic.OffendingField.Should().Be(nameof(NightSpawnComposition.EnemyArchetype));
    }

    [Fact]
    public void ShouldNotEmitUndefinedSpawnToken_WhenCompositionIsMalformed()
    {
        var waveManager = new WaveManager();
        var malformedComposition = new NightSpawnComposition(EnemyArchetype: " ", Count: 0);

        var result = waveManager.ProcessNightSpawn(malformedComposition);

        result.Emissions.Select(emission => emission.EnemyArchetype).Should().NotContain("UNDEFINED");
    }
}
