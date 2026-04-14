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

public class TechTreeReloadBehaviorTests
{
    // ACC:T17.15
    [Fact]
    [Trait("acceptance", "ACC:T17.15")]
    public void ShouldSwitchUnlockAvailabilityAndBarracksStats_WhenReloadingBetweenTwoValidTechTreeResources()
    {
        var firstResourceJson = BuildConfig(
            new TechNodeSeed("damage_20", "damage", 1.20d, Array.Empty<string>()),
            new TechNodeSeed("elite_blade", "damage", 1.10d, new[] { "damage_20" }));

        var secondResourceJson = BuildConfig(
            new TechNodeSeed("hp_10", "hp", 1.10d, Array.Empty<string>()),
            new TechNodeSeed("elite_blade", "damage", 1.10d, new[] { "missing_gate" }));

        var unlockedTechIds = new[] { "damage_20" };
        const int baseDamage = 20;

        var manager = TryCreateTechTreeManager(firstResourceJson, unlockedTechIds, out var managerType);
        var availabilityBeforeReload = TryIsAvailable(managerType, manager, "elite_blade");
        var damageMultiplierBeforeReload = GetStatMultiplier(managerType, manager, "damage");
        var trainedDamageBeforeReload = ApplyMultiplier(baseDamage, damageMultiplierBeforeReload);

        var reloadAccepted = TryReload(managerType, manager, secondResourceJson);
        if (managerType is not null && manager is not null)
        {
            ApplyUnlockedNodes(managerType, manager, unlockedTechIds);
        }

        var availabilityAfterReload = TryIsAvailable(managerType, manager, "elite_blade");
        var damageMultiplierAfterReload = GetStatMultiplier(managerType, manager, "damage");
        var trainedDamageAfterReload = ApplyMultiplier(baseDamage, damageMultiplierAfterReload);

        reloadAccepted.Should().BeTrue("switching valid tech-tree resources must be supported without C# source edits.");
        availabilityBeforeReload.Should().BeTrue("the active first resource should expose an unlock path for elite_blade.");
        availabilityAfterReload.Should().BeFalse("after resource switch, elite_blade path should follow the active resource prerequisites.");
        trainedDamageBeforeReload.Should().Be(24, "damage modifier from first resource should affect barracks-trained unit stats.");
        trainedDamageAfterReload.Should().Be(20, "after reload, barracks-trained unit stats must follow the active second resource.");
    }

    // ACC:T17.26
    [Fact]
    [Trait("acceptance", "ACC:T17.26")]
    public void ShouldKeepTraceArtifactDeterministic_WhenReloadingSameResourceWithSameUnlockedState()
    {
        var resourceJson = BuildConfig(
            new TechNodeSeed("damage_10", "damage", 1.10d, Array.Empty<string>()),
            new TechNodeSeed("hp_25", "hp", 1.25d, Array.Empty<string>()));

        var unlockedTechIds = new[] { "damage_10", "hp_25" };

        var manager = TryCreateTechTreeManager(resourceJson, unlockedTechIds, out var managerType);
        var damageMultiplierBeforeReload = GetStatMultiplier(managerType, manager, "damage");
        var hitPointMultiplierBeforeReload = GetStatMultiplier(managerType, manager, "hp");
        var traceBeforeReload = TryReadTraceArtifact(managerType, manager);

        var reloadAccepted = TryReload(managerType, manager, resourceJson);
        if (managerType is not null && manager is not null)
        {
            ApplyUnlockedNodes(managerType, manager, unlockedTechIds);
        }

        var damageMultiplierAfterReload = GetStatMultiplier(managerType, manager, "damage");
        var hitPointMultiplierAfterReload = GetStatMultiplier(managerType, manager, "hp");
        var traceAfterReload = TryReadTraceArtifact(managerType, manager);

        reloadAccepted.Should().BeTrue("deterministic regression replay requires reloading the same resource.");
        damageMultiplierAfterReload.Should().BeApproximately(damageMultiplierBeforeReload, 0.0000001d);
        hitPointMultiplierAfterReload.Should().BeApproximately(hitPointMultiplierBeforeReload, 0.0000001d);
        traceBeforeReload.Should().NotBeNullOrWhiteSpace("regression outputs must expose deterministic trace artifacts.");
        traceBeforeReload.Should().Contain("damage_10", "trace must include unlock sequence details.");
        traceBeforeReload.Should().Contain("hp_25", "trace must include unlock sequence details.");
        traceAfterReload.Should().Be(traceBeforeReload, "trace artifacts must stay deterministic for audit and replay comparison.");
    }

    private static string BuildConfig(params TechNodeSeed[] nodes)
    {
        var payload = new
        {
            nodes = nodes.Select(node => new
            {
                id = node.Id,
                stat = node.Stat,
                multiplier = node.Multiplier,
                prerequisites = node.Prerequisites,
            }).ToArray(),
        };

        return JsonSerializer.Serialize(payload);
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

    private static void ApplyUnlockedNodes(Type managerType, object manager, IReadOnlyCollection<string> unlockedTechIds)
    {
        var setMethod = FindMethodWithSingleCollectionParameter(
            managerType,
            "SetUnlockedTechIds",
            "SetUnlockedNodes",
            "ReplaceUnlockedTechIds");

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

    private static bool TryIsAvailable(Type? managerType, object? manager, string techId)
    {
        if (managerType is null || manager is null)
        {
            return false;
        }

        var method = FindMethodWithSingleStringParameter(managerType, "IsAvailable", "CanUnlock");
        if (method is null)
        {
            return false;
        }

        return method.Invoke(manager, new object[] { techId }) is bool value && value;
    }

    private static double GetStatMultiplier(Type? managerType, object? manager, string statKey)
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

    private static string TryReadTraceArtifact(Type? managerType, object? manager)
    {
        if (managerType is null || manager is null)
        {
            return string.Empty;
        }

        var traceMethodNames = new[]
        {
            "GetDeterministicTrace",
            "ExportDeterministicTrace",
            "GetApplicationTrace",
            "ExportApplicationTrace",
            "GetTraceArtifact",
            "ExportTraceArtifact",
            "BuildTraceArtifact"
        };

        var method = managerType
            .GetMethods()
            .FirstOrDefault(candidate =>
                traceMethodNames.Contains(candidate.Name, StringComparer.Ordinal) &&
                candidate.GetParameters().Length == 0);

        if (method is null)
        {
            return string.Empty;
        }

        var rawTrace = method.Invoke(manager, Array.Empty<object>());
        return NormalizeTrace(rawTrace);
    }

    private static string NormalizeTrace(object? rawTrace)
    {
        return rawTrace switch
        {
            null => string.Empty,
            string text => text,
            IEnumerable sequence => string.Join("|", sequence.Cast<object?>().Select(item => item?.ToString() ?? string.Empty)),
            _ => JsonSerializer.Serialize(rawTrace),
        };
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

    private static MethodInfo? FindMethodWithSingleCollectionParameter(Type managerType, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var method = managerType
                .GetMethods()
                .FirstOrDefault(candidate =>
                    candidate.Name == methodName &&
                    candidate.GetParameters().Length == 1 &&
                    typeof(IEnumerable).IsAssignableFrom(candidate.GetParameters()[0].ParameterType));

            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    private static int ApplyMultiplier(int value, double multiplier)
    {
        return (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);
    }

    private sealed record TechNodeSeed(string Id, string Stat, double Multiplier, IReadOnlyList<string> Prerequisites);
}
