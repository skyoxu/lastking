using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResidenceTaxConfigTests
{
    // ACC:T14.6
    [Fact]
    [Trait("acceptance", "ACC:T14.6")]
    public void ShouldUseLevelSpecificTaxPerTick_WhenResidenceLevelOverrideExists()
    {
        var json = CreateBalanceJsonWithResidenceBody(
            """
            "tax_per_tick": 50,
            "tax_per_tick_by_level": {
              "2": 55,
              "3": 60
            }
            """);

        var manager = new ConfigManager();
        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue("baseline balance payload should remain valid before residence tax extraction.");
        var taxPerTick = ResolveTaxPerTickForLevel(result.Snapshot, residenceLevel: 3);

        taxPerTick.Should().Be(60, "level-specific residence tax must come from config when level override exists.");
    }

    // ACC:T14.6
    [Fact]
    [Trait("acceptance", "ACC:T14.6")]
    public void ShouldUseBaseTaxPerTick_WhenResidenceLevelOverrideIsMissing()
    {
        var json = CreateBalanceJsonWithResidenceBody(
            """
            "tax_per_tick": 50,
            "tax_per_tick_by_level": {
              "2": 55
            }
            """);

        var manager = new ConfigManager();
        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue("baseline balance payload should remain valid before residence tax extraction.");
        var taxPerTick = ResolveTaxPerTickForLevel(result.Snapshot, residenceLevel: 4);

        taxPerTick.Should().Be(50, "missing level override must fall back to configured base residence tax.");
    }

    // ACC:T14.20
    [Theory]
    [Trait("acceptance", "ACC:T14.20")]
    [InlineData("\"tax_per_tick\": 12.5")]
    [InlineData("\"tax_per_tick\": 50, \"tax_per_tick_by_level\": { \"2\": 12.25 }")]
    public void ShouldRejectNonIntegerResidenceEconomyConfig_WhenAnyTaxFieldUsesFractionalNumber(string residenceBody)
    {
        var json = CreateBalanceJsonWithResidenceBody(residenceBody);
        var manager = new ConfigManager();

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse("residence economy contract is integer-only and must reject fractional tax values.");
        result.ReasonCodes.Should().Contain(
            ConfigManager.InvalidTypeReason,
            "non-integer residence tax fields must fail with explicit invalid-type reason.");
        result.ReasonCodes.Should().NotBeEmpty("rejection path must provide explicit reason codes for auditability.");
        result.Snapshot.Should().Be(BalanceSnapshot.Default, "failed load should keep fallback snapshot unchanged.");
    }

    private static int ResolveTaxPerTickForLevel(object snapshot, int residenceLevel)
    {
        var resolverMethod = FindResolverMethod(snapshot.GetType());
        if (resolverMethod is not null)
        {
            var resolved = resolverMethod.Invoke(snapshot, new object[] { residenceLevel });
            resolved.Should().BeOfType<int>("residence tax resolver must return an integer tax per tick.");
            return (int)resolved!;
        }

        var levelOverrideFound = TryReadLevelOverride(snapshot, residenceLevel, out var levelTax);
        var baseTax = TryReadIntProperty(
            snapshot,
            "ResidenceTaxPerTick",
            "ResidenceBaseTaxPerTick",
            "ResidenceTaxGoldPerTick");

        (levelOverrideFound || baseTax.HasValue).Should().BeTrue(
            "snapshot must expose residence tax config either via resolver method or integer properties.");

        return levelOverrideFound ? levelTax : baseTax!.Value;
    }

    private static MethodInfo? FindResolverMethod(Type snapshotType)
    {
        var candidateNames = new[]
        {
            "GetResidenceTaxPerTickForLevel",
            "ResolveResidenceTaxPerTickForLevel",
            "ResolveResidenceTaxPerTick",
            "GetResidenceTaxByLevel"
        };

        return candidateNames
            .Select(name => snapshotType.GetMethod(name, new[] { typeof(int) }))
            .FirstOrDefault(method => method is not null);
    }

    private static bool TryReadLevelOverride(object snapshot, int residenceLevel, out int levelTax)
    {
        var candidateNames = new[]
        {
            "ResidenceTaxPerTickByLevel",
            "ResidenceTaxByLevel",
            "ResidenceLevelTaxByLevel"
        };

        foreach (var candidateName in candidateNames)
        {
            var property = snapshot.GetType().GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            var mapping = property.GetValue(snapshot);
            if (TryResolveFromMapping(mapping, residenceLevel, out levelTax))
            {
                return true;
            }
        }

        levelTax = default;
        return false;
    }

    private static bool TryResolveFromMapping(object? mapping, int residenceLevel, out int levelTax)
    {
        if (mapping is IReadOnlyDictionary<int, int> readOnlyIntMap && readOnlyIntMap.TryGetValue(residenceLevel, out levelTax))
        {
            return true;
        }

        if (mapping is IDictionary<int, int> intMap && intMap.TryGetValue(residenceLevel, out levelTax))
        {
            return true;
        }

        var levelKey = residenceLevel.ToString(CultureInfo.InvariantCulture);

        if (mapping is IReadOnlyDictionary<string, int> readOnlyStringMap && readOnlyStringMap.TryGetValue(levelKey, out levelTax))
        {
            return true;
        }

        if (mapping is IDictionary<string, int> stringMap && stringMap.TryGetValue(levelKey, out levelTax))
        {
            return true;
        }

        if (mapping is IDictionary dictionary)
        {
            if (dictionary.Contains(residenceLevel) && dictionary[residenceLevel] is int intKeyValue)
            {
                levelTax = intKeyValue;
                return true;
            }

            if (dictionary.Contains(levelKey) && dictionary[levelKey] is int stringKeyValue)
            {
                levelTax = stringKeyValue;
                return true;
            }
        }

        levelTax = default;
        return false;
    }

    private static int? TryReadIntProperty(object instance, params string[] candidateNames)
    {
        foreach (var candidateName in candidateNames)
        {
            var property = instance.GetType().GetProperty(candidateName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property?.PropertyType == typeof(int))
            {
                return (int)property.GetValue(instance)!;
            }
        }

        return null;
    }

    private static string CreateBalanceJsonWithResidenceBody(string residenceBody)
    {
        return $$"""
{
  "time": {
    "day_seconds": 240,
    "night_seconds": 120
  },
  "waves": {
    "normal": {
      "day1_budget": 50,
      "daily_growth": 1.2
    }
  },
  "channels": {
    "elite": "elite",
    "boss": "boss"
  },
  "spawn": {
    "cadence_seconds": 10
  },
  "boss": {
    "count": 2
  },
  "economy": {
    "residence": {
      {{residenceBody}}
    }
  }
}
""";
    }
}
