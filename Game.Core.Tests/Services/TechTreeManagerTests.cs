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

public class TechTreeManagerTests
{
    // ACC:T17.21
    [Fact]
    [Trait("acceptance", "ACC:T17.21")]
    public void ShouldRejectOutOfBoundsPercentageModifiersAndKeepDeterministicOrder_WhenApplyingSchemaValidatedRuntimeModifiers()
    {
        var orderedConfig = BuildConfig(("damage_10", "damage", 1.10), ("damage_20", "damage", 1.20));
        var reorderedConfig = BuildConfig(("damage_20", "damage", 1.20), ("damage_10", "damage", 1.10));
        var outOfBoundsConfig = BuildConfig(("damage_400", "damage", 5.00));

        var orderedMultiplier = EvaluateMultiplier(orderedConfig, new[] { "damage_10", "damage_20" }, "damage");
        var reorderedMultiplier = EvaluateMultiplier(reorderedConfig, new[] { "damage_10", "damage_20" }, "damage");
        var invalidMultiplier = EvaluateMultiplier(outOfBoundsConfig, new[] { "damage_400" }, "damage");

        invalidMultiplier.Should().Be(1.0d, "out-of-bounds percentage modifiers must be rejected.");
        orderedMultiplier.Should().BeGreaterThan(1.0d, "validated modifiers must affect runtime results.");
        reorderedMultiplier.Should().BeApproximately(orderedMultiplier, 0.0000001d, "apply order must be deterministic for the same unlocked-node set.");
    }

    // ACC:T17.23
    [Fact]
    [Trait("acceptance", "ACC:T17.23")]
    public void ShouldUseActiveConfigValues_WhenReloadingWithModifierValueChangesOnly()
    {
        var initialConfig = BuildConfig(("damage_runtime", "damage", 1.10));
        var reloadedConfig = BuildConfig(("damage_runtime", "damage", 1.25));
        var unlockedNodeIds = new[] { "damage_runtime" };

        var manager = TryCreateTechTreeManager(initialConfig, unlockedNodeIds, out var managerType);
        var beforeReload = GetMultiplier(managerType, manager, "damage");
        var reloadApplied = TryReload(managerType, manager, reloadedConfig);
        var afterReload = GetMultiplier(managerType, manager, "damage");

        reloadApplied.Should().BeTrue("runtime must support value-only config reload.");
        beforeReload.Should().BeApproximately(1.10d, 0.0000001d);
        afterReload.Should().BeApproximately(1.25d, 0.0000001d);
        afterReload.Should().NotBe(beforeReload, "reloaded config values must replace old multiplier values.");
    }

    private static string BuildConfig(params (string id, string stat, double multiplier)[] nodes)
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

    private static double EvaluateMultiplier(string jsonConfig, IReadOnlyCollection<string> unlockedTechIds, string statKey)
    {
        var manager = TryCreateTechTreeManager(jsonConfig, unlockedTechIds, out var managerType);
        return GetMultiplier(managerType, manager, statKey);
    }

    private static object? TryCreateTechTreeManager(string jsonConfig, IReadOnlyCollection<string> unlockedTechIds, out Type? managerType)
    {
        managerType = typeof(BarracksTrainingQueueRuntime).Assembly.GetType("Game.Core.Services.TechTreeManager");
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

    private static bool TryReload(Type? managerType, object? manager, string jsonConfig)
    {
        if (managerType is null || manager is null)
        {
            return false;
        }

        return TryInvokeLoadLike(managerType, manager, jsonConfig, preferReload: true);
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

    private static double GetMultiplier(Type? managerType, object? manager, string statKey)
    {
        if (managerType is null || manager is null)
        {
            return 1.0d;
        }

        var method = FindMethodWithSingleStringParameter(managerType, "GetStatMultiplier", "GetMultiplier");
        if (method is null)
        {
            return 1.0d;
        }

        var rawValue = method.Invoke(manager, new object[] { statKey });
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

    private static void ApplyUnlockedNodes(Type managerType, object manager, IReadOnlyCollection<string> unlockedTechIds)
    {
        var setMethod = managerType.GetMethods()
            .FirstOrDefault(candidate =>
                (candidate.Name == "SetUnlockedTechIds" || candidate.Name == "SetUnlockedNodes" || candidate.Name == "ReplaceUnlockedTechIds") &&
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
}
