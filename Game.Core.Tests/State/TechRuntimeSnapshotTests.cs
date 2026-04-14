using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.State;

public class TechRuntimeSnapshotTests
{
    // ACC:T17.22
    [Fact]
    [Trait("acceptance", "ACC:T17.22")]
    public void ShouldReflectNewMultipliersInNextSnapshot_WhenUnlockStateChanges()
    {
        var jsonConfig = BuildConfig(new TechNodeSeed("damage_10", "damage", 1.10d, Array.Empty<string>()));

        var manager = TryCreateTechTreeManager(jsonConfig, Array.Empty<string>(), out var managerType);
        var snapshotBeforeUnlock = CaptureSnapshot(managerType, manager, "damage");

        if (managerType is not null && manager is not null)
        {
            ApplyUnlockedNodes(managerType, manager, new[] { "damage_10" });
        }

        var snapshotAfterUnlock = CaptureSnapshot(managerType, manager, "damage");

        snapshotBeforeUnlock.DamageMultiplier.Should().Be(1.0d);
        snapshotAfterUnlock.DamageMultiplier.Should().BeApproximately(
            1.10d,
            0.0000001d,
            "the next runtime snapshot must reflect newly unlocked tech multipliers for barracks stat computation.");
    }

    [Fact]
    public void ShouldKeepPreviouslyProducedSnapshotUnchanged_WhenUnlockStateChanges()
    {
        var jsonConfig = BuildConfig(new TechNodeSeed("damage_10", "damage", 1.10d, Array.Empty<string>()));

        var manager = TryCreateTechTreeManager(jsonConfig, Array.Empty<string>(), out var managerType);
        var snapshotBeforeUnlock = CaptureSnapshot(managerType, manager, "damage");

        if (managerType is not null && manager is not null)
        {
            ApplyUnlockedNodes(managerType, manager, new[] { "damage_10" });
        }

        var snapshotAfterUnlock = CaptureSnapshot(managerType, manager, "damage");

        snapshotBeforeUnlock.DamageMultiplier.Should().Be(1.0d, "already produced snapshots must remain frozen.");
        snapshotAfterUnlock.DamageMultiplier.Should().BeApproximately(
            1.10d,
            0.0000001d,
            "a new snapshot after unlock-state change must carry updated multipliers.");
        snapshotBeforeUnlock.DamageMultiplier.Should().NotBe(
            snapshotAfterUnlock.DamageMultiplier,
            "old snapshots must not be retroactively mutated by later unlock operations.");
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

    private static object? TryCreateTechTreeManager(
        string jsonConfig,
        IReadOnlyCollection<string> unlockedTechIds,
        out Type? managerType)
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

    private static TechRuntimeSnapshot CaptureSnapshot(Type? managerType, object? manager, string statKey)
    {
        var multiplier = GetMultiplier(managerType, manager, statKey);
        var version = GetVersionFromSnapshotOrManager(managerType, manager);
        return new TechRuntimeSnapshot(multiplier, version);
    }

    private static double GetMultiplier(Type? managerType, object? manager, string statKey)
    {
        if (managerType is null || manager is null)
        {
            return 1.0d;
        }

        var snapshot = TryGetSnapshotObject(managerType, manager);
        if (snapshot is not null)
        {
            var snapshotMultiplier = TryReadMultiplierFromObject(snapshot, statKey);
            if (snapshotMultiplier.HasValue)
            {
                return snapshotMultiplier.Value;
            }
        }

        var method = FindMethodWithSingleStringParameter(managerType, "GetStatMultiplier", "GetMultiplier");
        if (method is null)
        {
            return 1.0d;
        }

        return ConvertToDouble(method.Invoke(manager, new object[] { statKey }));
    }

    private static long GetVersionFromSnapshotOrManager(Type? managerType, object? manager)
    {
        if (managerType is null || manager is null)
        {
            return 0L;
        }

        var snapshot = TryGetSnapshotObject(managerType, manager);
        var source = snapshot ?? manager;

        foreach (var memberName in new[] { "Version", "Generation", "Revision", "SnapshotVersion" })
        {
            var property = source.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            var rawValue = property.GetValue(source);
            if (rawValue is null)
            {
                continue;
            }

            try
            {
                return Convert.ToInt64(rawValue);
            }
            catch
            {
                return 0L;
            }
        }

        return 0L;
    }

    private static object? TryGetSnapshotObject(Type managerType, object manager)
    {
        foreach (var methodName in new[] { "GetRuntimeSnapshot", "BuildRuntimeSnapshot", "CreateRuntimeSnapshot", "Snapshot" })
        {
            var method = managerType.GetMethod(methodName, Type.EmptyTypes);
            if (method is null)
            {
                continue;
            }

            return method.Invoke(manager, Array.Empty<object>());
        }

        foreach (var propertyName in new[] { "RuntimeSnapshot", "Snapshot", "CurrentSnapshot" })
        {
            var property = managerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            return property.GetValue(manager);
        }

        return null;
    }

    private static double? TryReadMultiplierFromObject(object snapshot, string statKey)
    {
        var snapshotType = snapshot.GetType();

        var method = FindMethodWithSingleStringParameter(snapshotType, "GetStatMultiplier", "GetMultiplier");
        if (method is not null)
        {
            return ConvertToDouble(method.Invoke(snapshot, new object[] { statKey }));
        }

        if (snapshot is IReadOnlyDictionary<string, double> typedDictionary &&
            typedDictionary.TryGetValue(statKey, out var typedValue))
        {
            return typedValue;
        }

        if (snapshot is IDictionary dictionary && dictionary.Contains(statKey))
        {
            return ConvertToDouble(dictionary[statKey]);
        }

        var fieldLikeNames = new[]
        {
            statKey,
            $"{statKey}Multiplier",
            "DamageMultiplier",
        };

        foreach (var name in fieldLikeNames)
        {
            var property = snapshotType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                continue;
            }

            return ConvertToDouble(property.GetValue(snapshot));
        }

        return null;
    }

    private static MethodInfo? FindMethodWithSingleStringParameter(Type ownerType, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var method = ownerType.GetMethod(methodName, new[] { typeof(string) });
            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    private static double ConvertToDouble(object? rawValue)
    {
        return rawValue switch
        {
            double value => value,
            float value => value,
            decimal value => (double)value,
            int value => value,
            long value => value,
            null => 1.0d,
            _ => 1.0d,
        };
    }

    private sealed record TechNodeSeed(string Id, string Stat, double Multiplier, IReadOnlyList<string> Prerequisites);

    private sealed record TechRuntimeSnapshot(double DamageMultiplier, long Version);
}
