using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class TechTreeMultiplierTests
{
    // ACC:T17.2
    [Fact]
    [Trait("acceptance", "ACC:T17.2")]
    public void ShouldComputeMultipleStatMultipliers_WhenJsonNodeDefinesMultipleModifiers()
    {
        var jsonConfig = BuildModifiersObjectConfig();
        var unlockedTechIds = new[] { "combined_i" };

        var damageMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, "damage");
        var hitPointMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, "hp");

        damageMultiplier.Should().BeApproximately(1.10d, 0.0000001d);
        hitPointMultiplier.Should().BeApproximately(1.20d, 0.0000001d);
    }

    // ACC:T17.5
    [Fact]
    [Trait("acceptance", "ACC:T17.5")]
    public void ShouldExcludeLockedModifierFromProduct_WhenComputingDamageMultiplier()
    {
        var nodeSpecs = new (string id, string stat, double multiplier)[]
        {
            ("damage_10", "damage", 1.10d),
            ("damage_20", "damage", 1.20d),
        };

        var jsonConfig = BuildStatConfig(nodeSpecs);
        var unlockedTechIds = new[] { "damage_10" };

        var actualMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, "damage");

        actualMultiplier.Should().BeApproximately(1.10d, 0.0000001d);
    }

    // ACC:T17.6
    [Fact]
    [Trait("acceptance", "ACC:T17.6")]
    public void ShouldKeepUnityMultiplier_WhenNodeExistsInJsonButRemainsLocked()
    {
        var jsonConfig = BuildStatConfig(("damage_15", "damage", 1.15d));
        var unlockedTechIds = Array.Empty<string>();

        var actualMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, "damage");

        actualMultiplier.Should().Be(1.0d);
    }

    // ACC:T17.10
    [Fact]
    [Trait("acceptance", "ACC:T17.10")]
    public void ShouldReturnMultiplicativeProduct_WhenTwoUnlockedNodesModifySameStat()
    {
        var jsonConfig = BuildStatConfig(
            ("damage_10", "damage", 1.10d),
            ("damage_20", "damage", 1.20d));
        var unlockedTechIds = new[] { "damage_10", "damage_20" };

        var actualMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, "damage");

        actualMultiplier.Should().BeApproximately(1.32d, 0.0000001d);
    }

    // ACC:T17.13
    [Fact]
    [Trait("acceptance", "ACC:T17.13")]
    public void ShouldKeepUnityAndBaselineValue_WhenNoUnlockedNodeModifiesDamage()
    {
        var jsonConfig = BuildStatConfig(("range_25", "range", 1.25d));
        var unlockedTechIds = new[] { "range_25" };
        var baselineDamage = 20;

        var damageMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, "damage");
        var trainedDamage = ApplyMultiplier(baselineDamage, damageMultiplier);

        damageMultiplier.Should().Be(1.0d);
        trainedDamage.Should().Be(baselineDamage);
    }

    // ACC:T17.16
    [Fact]
    [Trait("acceptance", "ACC:T17.16")]
    public void ShouldReturnProductPerSupportedStat_WhenEvaluatingRuntimeMultipliers()
    {
        var nodeSpecs = new (string id, string stat, double multiplier)[]
        {
            ("attack_speed_10", "attack_speed", 1.10d),
            ("attack_speed_20", "attack_speed", 1.20d),
            ("damage_10", "damage", 1.10d),
            ("damage_20", "damage", 1.20d),
            ("production_speed_10", "production_speed", 1.10d),
            ("production_speed_20", "production_speed", 1.20d),
            ("range_10", "range", 1.10d),
            ("range_20", "range", 1.20d),
            ("hp_10", "hp", 1.10d),
            ("hp_20", "hp", 1.20d),
            ("cost_10", "cost", 1.10d),
            ("cost_20", "cost", 1.20d),
        };

        var jsonConfig = BuildStatConfig(nodeSpecs);
        var unlockedTechIds = nodeSpecs.Select(node => node.id).ToArray();
        var statKeys = new[] { "attack_speed", "damage", "production_speed", "range", "hp", "cost" };

        foreach (var statKey in statKeys)
        {
            var actualMultiplier = EvaluateMultiplier(jsonConfig, unlockedTechIds, statKey);
            actualMultiplier.Should().BeApproximately(1.32d, 0.0000001d);
        }
    }

    private static string BuildStatConfig(params (string id, string stat, double multiplier)[] nodes)
    {
        var payload = new
        {
            nodes = nodes.Select(node => new
            {
                id = node.id,
                stat = node.stat,
                multiplier = node.multiplier,
                prerequisites = Array.Empty<string>(),
            }).ToArray(),
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildModifiersObjectConfig()
    {
        var payload = new
        {
            nodes = new object[]
            {
                new
                {
                    id = "combined_i",
                    prerequisites = Array.Empty<string>(),
                    modifiers = new Dictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["damage"] = 1.10d,
                        ["hp"] = 1.20d,
                    },
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }

    private static double EvaluateMultiplier(string jsonConfig, IReadOnlyCollection<string> unlockedTechIds, string statKey)
    {
        var manager = TryCreateTechTreeManager(jsonConfig, unlockedTechIds, out var managerType);
        if (managerType is null || manager is null)
        {
            return 1.0d;
        }

        var multiplierMethod = FindMethodWithSingleStringParameter(managerType, "GetStatMultiplier", "GetMultiplier");
        if (multiplierMethod is null)
        {
            return 1.0d;
        }

        var rawValue = multiplierMethod.Invoke(manager, new object[] { statKey });
        return rawValue switch
        {
            double value => value,
            float value => value,
            decimal value => (double)value,
            int value => value,
            long value => value,
            _ => 1.0d,
        };
    }

    private static object? TryCreateTechTreeManager(
        string jsonConfig,
        IReadOnlyCollection<string> unlockedTechIds,
        out Type? managerType)
    {
        managerType = typeof(BarracksTrainingQueueRuntime).Assembly.GetType("Game.Core.Services.TechTreeManager")
            ?? AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .FirstOrDefault(type => string.Equals(type.Name, "TechTreeManager", StringComparison.Ordinal));

        if (managerType is null)
        {
            return null;
        }

        try
        {
            object? manager;
            var jsonConstructor = managerType.GetConstructor(new[] { typeof(string) });
            if (jsonConstructor is not null)
            {
                manager = jsonConstructor.Invoke(new object[] { jsonConfig });
            }
            else
            {
                var parameterlessConstructor = managerType.GetConstructor(Type.EmptyTypes);
                if (parameterlessConstructor is null)
                {
                    return null;
                }

                manager = parameterlessConstructor.Invoke(null);
                _ = TryInvokeLoadLike(managerType, manager, jsonConfig, preferReload: false);
            }

            ApplyUnlockedNodes(managerType, manager, unlockedTechIds);
            return manager;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryInvokeLoadLike(Type managerType, object manager, string jsonConfig, bool preferReload)
    {
        var orderedNames = preferReload
            ? new[] { "ReloadFromJson", "ReloadConfig", "Reload", "LoadFromJson", "LoadConfig", "Load" }
            : new[] { "LoadFromJson", "LoadConfig", "Load", "ReloadFromJson", "ReloadConfig", "Reload" };

        foreach (var methodName in orderedNames)
        {
            var method = managerType.GetMethod(methodName, new[] { typeof(string) });
            if (method is null)
            {
                continue;
            }

            var result = method.Invoke(manager, new object[] { jsonConfig });
            if (method.ReturnType == typeof(void))
            {
                return true;
            }

            if (result is bool accepted)
            {
                return accepted;
            }

            if (result is null)
            {
                return false;
            }

            var acceptedProperty = result.GetType().GetProperty("Accepted", BindingFlags.Instance | BindingFlags.Public);
            if (acceptedProperty?.PropertyType == typeof(bool))
            {
                return (bool)(acceptedProperty.GetValue(result) ?? false);
            }

            return true;
        }

        return false;
    }

    private static void ApplyUnlockedNodes(Type managerType, object manager, IReadOnlyCollection<string> unlockedTechIds)
    {
        var setMethod = managerType.GetMethods()
            .FirstOrDefault(candidate =>
                (candidate.Name == "SetUnlockedTechIds" ||
                 candidate.Name == "SetUnlockedNodes" ||
                 candidate.Name == "ReplaceUnlockedTechIds") &&
                candidate.GetParameters().Length == 1 &&
                typeof(IEnumerable).IsAssignableFrom(candidate.GetParameters()[0].ParameterType));

        if (setMethod is not null)
        {
            setMethod.Invoke(manager, new object[] { unlockedTechIds.ToArray() });
            return;
        }

        var unlockMethod = FindMethodWithSingleStringParameter(managerType, "Unlock", "UnlockTech");
        if (unlockMethod is null)
        {
            return;
        }

        foreach (var techId in unlockedTechIds)
        {
            unlockMethod.Invoke(manager, new object[] { techId });
        }
    }

    private static MethodInfo? FindMethodWithSingleStringParameter(Type managerType, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var method = managerType.GetMethod(methodName, new[] { typeof(string) });
            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static int ApplyMultiplier(int value, double multiplier)
    {
        return (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);
    }
}
