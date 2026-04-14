using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public class TechTreeManagerJsonLoadingTests
{
    // ACC:T17.2
    [Fact]
    public void ShouldReplaceInMemoryNodeSet_WhenActiveJsonResourceChanges()
    {
        var firstJson = "{ \"nodes\": [ { \"id\": \"INFANTRY_I\", \"prerequisites\": [], \"modifiers\": { \"attack\": 1.10 } } ] }";
        var secondJson = "{ \"nodes\": [ { \"id\": \"ARCHERY_I\", \"prerequisites\": [], \"modifiers\": { \"range\": 1.15 } }, { \"id\": \"CAVALRY_I\", \"prerequisites\": [], \"modifiers\": { \"speed\": 1.20 } } ] }";

        var manager = CreateManagerInstance();
        LoadSnapshot(manager, firstJson);
        var snapshot = LoadSnapshot(manager, secondJson);

        var actualNodes = ReadNodeViews(snapshot);

        actualNodes.Keys.Should().BeEquivalentTo(new[] { "ARCHERY_I", "CAVALRY_I" });
        actualNodes.Keys.Should().NotContain("INFANTRY_I");
    }

    // ACC:T17.4
    [Fact]
    public void ShouldMatchNodeIdsAndPrerequisiteEdges_WhenParsingJsonResource()
    {
        var json = "{ \"nodes\": [ { \"id\": \"INFANTRY_I\", \"prerequisites\": [], \"modifiers\": { \"attack\": 1.05 } }, { \"id\": \"INFANTRY_II\", \"prerequisites\": [\"INFANTRY_I\"], \"modifiers\": { \"attack\": 1.10 } }, { \"id\": \"PHALANX\", \"prerequisites\": [\"INFANTRY_I\", \"INFANTRY_II\"], \"modifiers\": { \"defense\": 1.20 } } ] }";

        var snapshot = LoadSnapshot(CreateManagerInstance(), json);
        var expectedNodes = ParseExpectedNodes(json);
        var actualNodes = ReadNodeViews(snapshot);

        actualNodes.Keys.Should().BeEquivalentTo(expectedNodes.Keys);
        foreach (var expectedNode in expectedNodes.Values)
        {
            actualNodes.Should().ContainKey(expectedNode.Id);
            actualNodes[expectedNode.Id].Prerequisites.Should().BeEquivalentTo(expectedNode.Prerequisites);
        }
    }

    // ACC:T17.9
    [Fact]
    public void ShouldMatchModifierValues_WhenParsingJsonResource()
    {
        var json = "{ \"nodes\": [ { \"id\": \"ARCHERY_I\", \"prerequisites\": [], \"modifiers\": { \"range\": 1.125, \"reload\": 0.95 } }, { \"id\": \"ARCHERY_II\", \"prerequisites\": [\"ARCHERY_I\"], \"modifiers\": { \"range\": 1.250, \"reload\": 0.90 } } ] }";

        var snapshot = LoadSnapshot(CreateManagerInstance(), json);
        var expectedNodes = ParseExpectedNodes(json);
        var actualNodes = ReadNodeViews(snapshot);

        foreach (var expectedNode in expectedNodes.Values)
        {
            actualNodes.Should().ContainKey(expectedNode.Id);
            var actualNode = actualNodes[expectedNode.Id];

            actualNode.Modifiers.Keys.Should().BeEquivalentTo(expectedNode.Modifiers.Keys);
            foreach (var expectedModifier in expectedNode.Modifiers)
            {
                actualNode.Modifiers.Should().ContainKey(expectedModifier.Key);
                actualNode.Modifiers[expectedModifier.Key].Should().BeApproximately(expectedModifier.Value, 0.0000001);
            }
        }
    }

    private static object CreateManagerInstance()
    {
        TryLoadAssembly("Game.Core");

        var managerType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(SafeGetTypes)
            .FirstOrDefault(type => string.Equals(type.Name, "TechTreeManager", StringComparison.Ordinal));

        managerType.Should().NotBeNull("Tech tree runtime should expose a TechTreeManager type.");

        var constructor = managerType!.GetConstructor(Type.EmptyTypes);
        constructor.Should().NotBeNull("TechTreeManager should expose a parameterless constructor for deterministic tests.");

        return constructor!.Invoke(null)!;
    }

    private static object LoadSnapshot(object manager, string json)
    {
        var candidateMethodNames = new[]
        {
            "LoadFromJson",
            "LoadJson",
            "ReloadFromJson",
            "ApplyJson",
            "SetJson",
            "InitializeFromJson",
        };

        var loadMethod = manager
            .GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
                candidateMethodNames.Contains(method.Name, StringComparer.OrdinalIgnoreCase) &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(string));

        loadMethod.Should().NotBeNull("TechTreeManager should have a JSON loading method that accepts a single string parameter.");

        var result = loadMethod!.Invoke(manager, new object[] { json });
        if (result is not null)
        {
            return result;
        }

        var snapshot = GetMemberValue(manager, "CurrentSnapshot", "Snapshot", "RuntimeSnapshot", "GetSnapshot");
        snapshot.Should().NotBeNull("Loading JSON should expose a runtime snapshot.");

        return snapshot!;
    }

    private static IReadOnlyDictionary<string, NodeView> ParseExpectedNodes(string json)
    {
        using var document = JsonDocument.Parse(json);

        var nodes = new Dictionary<string, NodeView>(StringComparer.Ordinal);
        foreach (var nodeElement in document.RootElement.GetProperty("nodes").EnumerateArray())
        {
            var id = nodeElement.GetProperty("id").GetString();
            id.Should().NotBeNullOrWhiteSpace();

            var prerequisites = nodeElement.TryGetProperty("prerequisites", out var prerequisitesElement)
                ? prerequisitesElement.EnumerateArray()
                    .Select(element => element.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToArray()
                : Array.Empty<string>();

            var modifiers = new Dictionary<string, double>(StringComparer.Ordinal);
            if (nodeElement.TryGetProperty("modifiers", out var modifiersElement))
            {
                foreach (var property in modifiersElement.EnumerateObject())
                {
                    modifiers[property.Name] = property.Value.GetDouble();
                }
            }

            nodes[id!] = new NodeView(id!, prerequisites, modifiers);
        }

        return nodes;
    }

    private static IReadOnlyDictionary<string, NodeView> ReadNodeViews(object snapshot)
    {
        var nodesObject = GetMemberValue(snapshot, "Nodes", "TechNodes", "NodeSet");
        nodesObject.Should().NotBeNull("Runtime snapshot should expose a node collection.");

        nodesObject.Should().BeAssignableTo<IEnumerable>();
        var nodeViews = new Dictionary<string, NodeView>(StringComparer.Ordinal);

        foreach (var rawItem in (IEnumerable)nodesObject!)
        {
            if (rawItem is null)
            {
                continue;
            }

            var keyValue = TryReadKeyValuePair(rawItem);
            var nodeSource = keyValue.HasValue ? keyValue.Value.Value ?? rawItem : rawItem;
            var nodeId = TryReadStringMember(nodeSource, "Id", "NodeId", "TechId");

            if (string.IsNullOrWhiteSpace(nodeId) && keyValue.HasValue)
            {
                nodeId = keyValue.Value.Key;
            }

            nodeId.Should().NotBeNullOrWhiteSpace("Each runtime node should expose a stable id.");

            var prerequisites = ReadStringCollection(nodeSource, "Prerequisites", "PrerequisiteIds", "Requires");
            var modifiers = ReadDoubleDictionary(nodeSource, "Modifiers", "StatModifiers", "Multipliers");

            nodeViews[nodeId!] = new NodeView(nodeId!, prerequisites, modifiers);
        }

        return nodeViews;
    }

    private static IReadOnlyList<string> ReadStringCollection(object source, params string[] memberNames)
    {
        var value = GetMemberValue(source, memberNames);
        if (value is null)
        {
            return Array.Empty<string>();
        }

        value.Should().BeAssignableTo<IEnumerable>();

        return ((IEnumerable)value)
            .Cast<object?>()
            .Select(item => item?.ToString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, double> ReadDoubleDictionary(object source, params string[] memberNames)
    {
        var value = GetMemberValue(source, memberNames);
        if (value is null)
        {
            return new Dictionary<string, double>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, double>(StringComparer.Ordinal);

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                result[key] = ToDouble(entry.Value);
            }

            return result;
        }

        value.Should().BeAssignableTo<IEnumerable>();
        foreach (var item in (IEnumerable)value)
        {
            if (item is null)
            {
                continue;
            }

            var key = TryReadStringMember(item, "Key");
            var entryValue = GetMemberValue(item, "Value");

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = ToDouble(entryValue);
        }

        return result;
    }

    private static (string Key, object? Value)? TryReadKeyValuePair(object item)
    {
        var key = TryReadStringMember(item, "Key");
        var value = GetMemberValue(item, "Value");

        if (key is null && value is null)
        {
            return null;
        }

        return (key ?? string.Empty, value);
    }

    private static string? TryReadStringMember(object source, params string[] memberNames)
    {
        var value = GetMemberValue(source, memberNames);
        return value?.ToString();
    }

    private static object? GetMemberValue(object source, params string[] memberNames)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var sourceType = source.GetType();

        foreach (var memberName in memberNames)
        {
            var property = sourceType.GetProperty(memberName, flags);
            if (property is not null)
            {
                return property.GetValue(source);
            }

            var field = sourceType.GetField(memberName, flags);
            if (field is not null)
            {
                return field.GetValue(source);
            }

            var method = sourceType.GetMethod(memberName, flags, Type.DefaultBinder, Type.EmptyTypes, null);
            if (method is not null)
            {
                return method.Invoke(source, null);
            }
        }

        return null;
    }

    private static double ToDouble(object? value)
    {
        value.Should().NotBeNull();

        if (value is double doubleValue)
        {
            return doubleValue;
        }

        if (value is float floatValue)
        {
            return floatValue;
        }

        if (value is decimal decimalValue)
        {
            return (double)decimalValue;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        var numericText = value!.ToString();
        numericText.Should().NotBeNullOrWhiteSpace();

        return double.Parse(numericText!, CultureInfo.InvariantCulture);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private static void TryLoadAssembly(string assemblyName)
    {
        try
        {
            Assembly.Load(assemblyName);
        }
        catch
        {
            // Ignore load failures and rely on already loaded assemblies.
        }
    }

    private sealed class NodeView
    {
        public NodeView(string id, IReadOnlyList<string> prerequisites, IReadOnlyDictionary<string, double> modifiers)
        {
            Id = id;
            Prerequisites = prerequisites;
            Modifiers = modifiers;
        }

        public string Id { get; }

        public IReadOnlyList<string> Prerequisites { get; }

        public IReadOnlyDictionary<string, double> Modifiers { get; }
    }
}
