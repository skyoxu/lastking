using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public class TechTreeManagerContractTests
{
    // ACC:T17.17
    [Fact]
    [Trait("acceptance", "ACC:T17.17")]
    public void ShouldExposeTechTreeManagerType_WhenResolvingContract()
    {
        var managerType = FindType("TechTreeManager");

        managerType.Should().NotBeNull("TechTreeManager must exist as the enforced manager type.");
    }

    // ACC:T17.18
    [Fact]
    [Trait("acceptance", "ACC:T17.18")]
    public void ShouldComputeStatMultiplierFromUnlockedNodes_WhenNodeStateChanges()
    {
        var managerType = FindType("TechTreeManager");
        managerType.Should().NotBeNull();

        var methodInfo = managerType!.GetMethod("GetStatMultiplier", new[] { typeof(string) });
        methodInfo.Should().NotBeNull("TechTreeManager must expose GetStatMultiplier(string stat).");

        var nodeMapField = FindNodeMapField(managerType);
        nodeMapField.Should().NotBeNull("TechTreeManager must keep an internal TechNode map.");

        var manager = CreateManager(managerType);
        var nodeType = nodeMapField!.FieldType.GetGenericArguments()[1];

        var emptyMap = CreateNodeMap(nodeType);
        nodeMapField.SetValue(manager, emptyMap);
        var baselineResult = InvokeMultiplier(methodInfo!, manager, "attack");

        var lockedMap = CreateNodeMap(nodeType);
        lockedMap["n1"] = CreateNode(nodeType, "n1", "attack", 1.50, false);
        nodeMapField.SetValue(manager, lockedMap);
        var lockedResult = InvokeMultiplier(methodInfo, manager, "attack");

        var oneUnlockedMap = CreateNodeMap(nodeType);
        oneUnlockedMap["n1"] = CreateNode(nodeType, "n1", "attack", 1.50, true);
        nodeMapField.SetValue(manager, oneUnlockedMap);
        var oneUnlockedResult = InvokeMultiplier(methodInfo, manager, "attack");

        var twoUnlockedMap = CreateNodeMap(nodeType);
        twoUnlockedMap["n1"] = CreateNode(nodeType, "n1", "attack", 1.50, true);
        twoUnlockedMap["n2"] = CreateNode(nodeType, "n2", "attack", 1.20, true);
        nodeMapField.SetValue(manager, twoUnlockedMap);
        var twoUnlockedResult = InvokeMultiplier(methodInfo, manager, "attack");

        lockedResult.Should().Be(baselineResult, "locked nodes must not change the multiplier.");
        oneUnlockedResult.Should().NotBe(baselineResult, "an unlocked node must affect the multiplier.");
        twoUnlockedResult.Should().NotBe(oneUnlockedResult, "changing unlocked nodes must change computed output.");
    }

    // ACC:T17.20
    [Fact]
    [Trait("acceptance", "ACC:T17.20")]
    public void ShouldKeepPrivateIdKeyedNodeStorage_WhenInspectingStructure()
    {
        var managerType = FindType("TechTreeManager");
        managerType.Should().NotBeNull();

        var nodeMapField = FindNodeMapField(managerType!);

        nodeMapField.Should().NotBeNull("a private Dictionary<string, TechNode> map is required.");
        nodeMapField!.IsPrivate.Should().BeTrue("node storage must remain private.");
    }

    private static Type? FindType(string typeName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type => type.Name.Equals(typeName, StringComparison.Ordinal));
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

    private static FieldInfo? FindNodeMapField(Type managerType)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        return managerType
            .GetFields(flags)
            .FirstOrDefault(field =>
                field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                field.FieldType.GetGenericArguments()[0] == typeof(string) &&
                field.FieldType.GetGenericArguments()[1].Name == "TechNode");
    }

    private static object CreateManager(Type managerType)
    {
        var constructor = managerType.GetConstructor(Type.EmptyTypes);
        constructor.Should().NotBeNull("TechTreeManager should be creatable in contract tests.");

        return constructor!.Invoke(Array.Empty<object>());
    }

    private static IDictionary CreateNodeMap(Type nodeType)
    {
        var mapType = typeof(Dictionary<,>).MakeGenericType(typeof(string), nodeType);
        return (IDictionary)Activator.CreateInstance(mapType)!;
    }

    private static object CreateNode(Type nodeType, string id, string stat, double multiplier, bool isUnlocked)
    {
        var node = Activator.CreateInstance(nodeType);
        node.Should().NotBeNull("TechNode should be instantiable for runtime state tests.");

        SetProperty(node!, nodeType, "Id", id);
        SetProperty(node, nodeType, "Stat", stat);
        SetProperty(node, nodeType, "Multiplier", multiplier);
        SetProperty(node, nodeType, "IsUnlocked", isUnlocked);

        return node;
    }

    private static void SetProperty(object target, Type targetType, string propertyName, object value)
    {
        var propertyInfo = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        propertyInfo.Should().NotBeNull($"TechNode.{propertyName} is required for contract validation.");

        var converted = Convert.ChangeType(value, propertyInfo!.PropertyType, CultureInfo.InvariantCulture);
        propertyInfo.SetValue(target, converted);
    }

    private static double InvokeMultiplier(MethodInfo methodInfo, object manager, string stat)
    {
        var result = methodInfo.Invoke(manager, new object[] { stat });
        result.Should().NotBeNull();

        return result switch
        {
            double value => value,
            float value => value,
            decimal value => (double)value,
            int value => value,
            _ => throw new InvalidOperationException($"Unsupported return type: {result!.GetType().FullName}")
        };
    }
}
