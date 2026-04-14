using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class TechTreeUnlockPrerequisitesTests
{
    // ACC:T17.12
    [Fact]
    [Trait("acceptance", "ACC:T17.12")]
    public void ShouldKeepTargetUnavailableAndApplyOnlyApplicableMultipliers_WhenPrerequisitesAreMissing()
    {
        const string jsonConfig = """
        {"nodes":[{"id":"damage_15","stat":"damage","multiplier":1.15,"prerequisites":[]},{"id":"hp_20","stat":"hp","multiplier":1.20,"prerequisites":["damage_15","armory_1"]}]}
        """;
        var unlockedTechIds = new[] { "damage_15" };
        var baselineStats = new UnitStats(Damage: 20, HitPoints: 100);

        var evaluation = EvaluateUnitStats(jsonConfig, unlockedTechIds, baselineStats);
        var targetAvailable = IsTechAvailable(jsonConfig, unlockedTechIds, "hp_20");

        targetAvailable.Should().BeFalse();
        evaluation.DamageMultiplier.Should().Be(1.15d);
        evaluation.HitPointMultiplier.Should().Be(1.0d);
        evaluation.TrainedStats.Damage.Should().Be(23);
        evaluation.TrainedStats.HitPoints.Should().Be(100);
    }

    [Fact]
    public void ShouldIgnoreTamperedUnlockedState_WhenTargetPrerequisitesAreMissing()
    {
        const string jsonConfig = """
        {"nodes":[{"id":"damage_15","stat":"damage","multiplier":1.15,"prerequisites":[]},{"id":"hp_20","stat":"hp","multiplier":1.20,"prerequisites":["damage_15","armory_1"]}]}
        """;
        var tamperedUnlockedTechIds = new[] { "damage_15", "hp_20" };
        var baselineStats = new UnitStats(Damage: 20, HitPoints: 100);

        var evaluation = EvaluateUnitStats(jsonConfig, tamperedUnlockedTechIds, baselineStats);
        var targetAvailable = IsTechAvailable(jsonConfig, tamperedUnlockedTechIds, "hp_20");

        targetAvailable.Should().BeFalse("hp_20 still has unmet prerequisite armory_1.");
        evaluation.DamageMultiplier.Should().Be(1.15d);
        evaluation.HitPointMultiplier.Should().Be(1.0d, "unmet prerequisites must block the hp modifier even when unlock state is tampered.");
        evaluation.TrainedStats.Damage.Should().Be(23);
        evaluation.TrainedStats.HitPoints.Should().Be(100);
    }

    private static UnitStatEvaluation EvaluateUnitStats(
        string jsonConfig,
        IReadOnlyCollection<string> unlockedTechIds,
        UnitStats baselineStats)
    {
        var damageMultiplier = TryGetStatMultiplierFromRuntime(jsonConfig, unlockedTechIds, "damage");
        var hitPointMultiplier = TryGetStatMultiplierFromRuntime(jsonConfig, unlockedTechIds, "hp");

        var trainedStats = new UnitStats(
            Damage: ApplyMultiplier(baselineStats.Damage, damageMultiplier),
            HitPoints: ApplyMultiplier(baselineStats.HitPoints, hitPointMultiplier));

        return new UnitStatEvaluation(damageMultiplier, hitPointMultiplier, trainedStats);
    }

    private static bool IsTechAvailable(string jsonConfig, IReadOnlyCollection<string> unlockedTechIds, string techId)
    {
        var manager = TryCreateTechTreeManager(jsonConfig, unlockedTechIds, out var managerType);
        if (manager is null || managerType is null)
        {
            return false;
        }

        var availabilityMethod = FindMethodWithSingleStringParameter(managerType, "IsAvailable", "CanUnlock");
        if (availabilityMethod is null)
        {
            return false;
        }

        return availabilityMethod.Invoke(manager, new object[] { techId }) is bool value && value;
    }

    private static double TryGetStatMultiplierFromRuntime(
        string jsonConfig,
        IReadOnlyCollection<string> unlockedTechIds,
        string statKey)
    {
        var manager = TryCreateTechTreeManager(jsonConfig, unlockedTechIds, out var managerType);
        if (manager is null || managerType is null)
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
                var loadMethod = FindMethodWithSingleStringParameter(managerType, "LoadFromJson", "LoadConfig", "Load");
                loadMethod?.Invoke(manager, new object[] { jsonConfig });
            }

            ApplyUnlockedNodes(managerType, manager, unlockedTechIds);
            return manager;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyUnlockedNodes(Type managerType, object manager, IReadOnlyCollection<string> unlockedTechIds)
    {
        var setUnlockedMethod = FindMethodWithSingleCollectionParameter(
            managerType,
            "SetUnlockedTechIds",
            "SetUnlockedNodes",
            "ReplaceUnlockedTechIds");

        if (setUnlockedMethod is not null)
        {
            setUnlockedMethod.Invoke(manager, new object[] { unlockedTechIds.ToArray() });
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

    private static MethodInfo? FindMethodWithSingleCollectionParameter(Type managerType, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var method = managerType
                .GetMethods()
                .FirstOrDefault(candidate =>
                    candidate.Name == methodName &&
                    candidate.GetParameters().Length == 1 &&
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(candidate.GetParameters()[0].ParameterType));

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

    private sealed record UnitStats(int Damage, int HitPoints);

    private sealed record UnitStatEvaluation(
        double DamageMultiplier,
        double HitPointMultiplier,
        UnitStats TrainedStats);
}
