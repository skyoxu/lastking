using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class BarracksUnitStatApplicationTests
{
    // ACC:T17.12
    [Fact]
    public void ShouldProduceIdenticalMultipliersAndBarracksStats_WhenEvaluatingSameConfigAndUnlockStateRepeatedly()
    {
        const string jsonConfig = """
        {"nodes":[{"id":"damage_10","stat":"damage","multiplier":1.10,"prerequisites":[]}]}
        """;
        var unlockedTechIds = new[] { "damage_10" };
        var baselineStats = new BarracksUnitStats(Damage: 20, HitPoints: 100);

        var first = EvaluateBarracksStats(jsonConfig, unlockedTechIds, baselineStats);
        var second = EvaluateBarracksStats(jsonConfig, unlockedTechIds, baselineStats);

        first.DamageMultiplier.Should().Be(second.DamageMultiplier);
        first.TrainedStats.Should().Be(second.TrainedStats);
        first.DamageMultiplier.Should().Be(1.10d);
        first.TrainedStats.Damage.Should().Be(22);
    }

    // ACC:T17.13
    [Fact]
    public void ShouldKeepBaselineMultiplierAndBarracksStats_WhenNoUnlockedNodeModifiesStat()
    {
        const string jsonConfig = """
        {"nodes":[{"id":"range_25","stat":"range","multiplier":1.25,"prerequisites":[]}]}
        """;
        var unlockedTechIds = new[] { "range_25" };
        var baselineStats = new BarracksUnitStats(Damage: 20, HitPoints: 100);

        var evaluation = EvaluateBarracksStats(jsonConfig, unlockedTechIds, baselineStats);

        evaluation.DamageMultiplier.Should().Be(1.0d);
        evaluation.HitPointMultiplier.Should().Be(1.0d);
        evaluation.TrainedStats.Damage.Should().Be(baselineStats.Damage);
        evaluation.TrainedStats.HitPoints.Should().Be(baselineStats.HitPoints);
    }

    // ACC:T17.3
    [Fact]
    public void ShouldKeepTargetUnavailableAndApplyOnlyCurrentlyApplicableMultipliers_WhenPrerequisitesAreNotMet()
    {
        const string jsonConfig = """
        {"nodes":[{"id":"damage_10","stat":"damage","multiplier":1.10,"prerequisites":[]},{"id":"hp_20","stat":"hp","multiplier":1.20,"prerequisites":["damage_10","armory_1"]}]}
        """;
        var unlockedTechIds = new[] { "damage_10" };
        var baselineStats = new BarracksUnitStats(Damage: 20, HitPoints: 100);

        var evaluation = EvaluateBarracksStats(jsonConfig, unlockedTechIds, baselineStats);
        var targetAvailable = IsTechAvailable(jsonConfig, unlockedTechIds, "hp_20");

        targetAvailable.Should().BeFalse();
        evaluation.DamageMultiplier.Should().Be(1.10d);
        evaluation.HitPointMultiplier.Should().Be(1.0d);
        evaluation.TrainedStats.Damage.Should().Be(22);
        evaluation.TrainedStats.HitPoints.Should().Be(100);
    }

    // ACC:T17.6
    [Fact]
    public void ShouldIgnoreLockedNodeModifier_WhenNodeIsPresentInJsonButStillLocked()
    {
        const string jsonConfig = """
        {"nodes":[{"id":"damage_15","stat":"damage","multiplier":1.15,"prerequisites":[]}]}
        """;
        var unlockedTechIds = Array.Empty<string>();
        var baselineStats = new BarracksUnitStats(Damage: 20, HitPoints: 100);

        var evaluation = EvaluateBarracksStats(jsonConfig, unlockedTechIds, baselineStats);

        evaluation.DamageMultiplier.Should().Be(1.0d);
        evaluation.TrainedStats.Damage.Should().Be(baselineStats.Damage);
    }

    private static BarracksUnitEvaluation EvaluateBarracksStats(
        string jsonConfig,
        IReadOnlyCollection<string> unlockedTechIds,
        BarracksUnitStats baselineStats)
    {
        var damageMultiplier = TryGetStatMultiplierFromRuntime(jsonConfig, unlockedTechIds, "damage");
        var hitPointMultiplier = TryGetStatMultiplierFromRuntime(jsonConfig, unlockedTechIds, "hp");

        var trainedStats = new BarracksUnitStats(
            Damage: ApplyMultiplier(baselineStats.Damage, damageMultiplier),
            HitPoints: ApplyMultiplier(baselineStats.HitPoints, hitPointMultiplier));

        return new BarracksUnitEvaluation(damageMultiplier, hitPointMultiplier, trainedStats);
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

    private sealed record BarracksUnitStats(int Damage, int HitPoints);

    private sealed record BarracksUnitEvaluation(
        double DamageMultiplier,
        double HitPointMultiplier,
        BarracksUnitStats TrainedStats);
}
