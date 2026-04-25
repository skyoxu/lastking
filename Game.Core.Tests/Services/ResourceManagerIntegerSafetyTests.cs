using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

[Collection(ResourceManagerIsolationCollection.Name)]
public sealed class ResourceManagerIntegerSafetyTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ACC:T12.1
    // ACC:T12.13
    // ACC:T12.18
    // ACC:T44.5
    // ACC:T44.6
    [Fact]
    [Trait("acceptance", "ACC:T12.1")]
    [Trait("acceptance", "ACC:T12.13")]
    [Trait("acceptance", "ACC:T12.18")]
    public void ShouldRejectNonIntegerSnapshotAndKeepStateUnchanged_WhenRuntimeAssertionGateEvaluatesImport()
    {
        var sut = CreateResourceManager();
        var stateBefore = ReadState(sut);
        const string nonIntegerSnapshot = "{\"gold\":800.5,\"iron\":150,\"populationCap\":50}";

        var importResult = TryImportSnapshot(sut, nonIntegerSnapshot);
        var stateAfter = ReadState(sut);

        importResult.Accepted.Should().BeFalse("non-integer arithmetic must be rejected by runtime assertion gates.");
        importResult.FailureReason.Should().NotBeNullOrWhiteSpace("rejection must expose an auditable reason.");
        stateAfter.Should().Be(stateBefore, "rejected non-integer arithmetic must not commit resource state mutation.");
    }

    // ACC:T44.4
    // ACC:T44.5
    [Fact]
    public void ShouldFailStaticScan_WhenResourceMutationPathContainsFloatingPointTypes()
    {
        var resourceManagerPath = Path.Combine(RepoRoot, "Game.Core", "Services", "ResourceManager.cs");

        File.Exists(resourceManagerPath).Should().BeTrue("Task 12 requires a concrete ResourceManager implementation for static integer-safety scanning.");

        var source = File.ReadAllText(resourceManagerPath);
        var nonIntegerPattern = new Regex(
            @"\b(float|double|decimal)\b|[0-9]+\.[0-9]+",
            RegexOptions.CultureInvariant);

        var flaggedTokens = nonIntegerPattern.Matches(source)
            .Select(match => match.Value)
            .ToArray();

        flaggedTokens.Should().BeEmpty("static scan gate must reject non-integer arithmetic tokens in ResourceManager.cs.");
    }

    private static object CreateResourceManager()
    {
        var resourceManagerType =
            Type.GetType("Game.Core.Services.ResourceManager, Game.Core", throwOnError: false) ??
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Game.Core.Services.ResourceManager", throwOnError: false))
                .FirstOrDefault(type => type is not null);

        resourceManagerType.Should().NotBeNull("Task 12 requires Game.Core.Services.ResourceManager.");

        var constructor = resourceManagerType!.GetConstructor(Type.EmptyTypes);
        constructor.Should().NotBeNull("ResourceManager should expose a parameterless constructor for deterministic tests.");

        return constructor!.Invoke(null);
    }

    private static ResourceState ReadState(object sut)
    {
        if (TryReadStateFromObject(sut, out var directState))
        {
            return directState;
        }

        var snapshotMethod = FindOptionalMethod(sut, new[] { "GetState", "GetSnapshot", "ReadState" });
        if (snapshotMethod is not null)
        {
            var snapshot = snapshotMethod.Invoke(sut, null);
            snapshot.Should().NotBeNull("state accessor must return a snapshot object.");
            if (snapshot is not null && TryReadStateFromObject(snapshot, out var methodState))
            {
                return methodState;
            }
        }

        var snapshotProperty = sut.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property =>
                string.Equals(property.Name, "Snapshot", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "State", StringComparison.OrdinalIgnoreCase));
        if (snapshotProperty is not null)
        {
            var snapshot = snapshotProperty.GetValue(sut);
            snapshot.Should().NotBeNull("snapshot property must not be null.");
            if (snapshot is not null && TryReadStateFromObject(snapshot, out var propertyState))
            {
                return propertyState;
            }
        }

        throw new InvalidOperationException("Unable to read Gold/Iron/PopulationCap state from ResourceManager.");
    }

    private static ImportResult TryImportSnapshot(object sut, string snapshot)
    {
        var importMethod = FindOptionalMethod(
            sut,
            new[] { "TryImportSnapshot", "ImportSnapshot", "LoadSnapshot", "RestoreSnapshot" },
            typeof(string));

        importMethod.Should().NotBeNull(
            "runtime integer-safety gate requires snapshot import entrypoint to reject non-integer values.");

        try
        {
            var rawResult = importMethod!.Invoke(sut, new object[] { snapshot });
            return InterpretImportResult(rawResult);
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException ?? ex;
            return new ImportResult(false, $"{inner.GetType().Name}: {inner.Message}");
        }
    }

    private static ImportResult InterpretImportResult(object? rawResult)
    {
        if (rawResult is null)
        {
            return new ImportResult(true, "void return treated as accepted import");
        }

        if (rawResult is bool accepted)
        {
            return new ImportResult(accepted, accepted ? "accepted" : "rejected");
        }

        var resultType = rawResult.GetType();
        var acceptedProperty = resultType.GetProperty("Accepted") ?? resultType.GetProperty("Success");
        if (acceptedProperty?.PropertyType == typeof(bool))
        {
            var acceptedValue = (bool)acceptedProperty.GetValue(rawResult)!;
            var reasonProperty = resultType.GetProperty("Reason") ?? resultType.GetProperty("FailureReason");
            var reasonValue = reasonProperty?.GetValue(rawResult)?.ToString() ??
                              (acceptedValue ? "accepted" : "rejected");
            return new ImportResult(acceptedValue, reasonValue);
        }

        throw new InvalidOperationException(
            $"{resultType.Name} must expose bool Accepted/Success when used as import result.");
    }

    private static MethodInfo? FindOptionalMethod(object target, string[] names, params Type[] parameterTypes)
    {
        foreach (var name in names)
        {
            var method = target.GetType().GetMethod(name, parameterTypes);
            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    private static bool TryReadStateFromObject(object instance, out ResourceState state)
    {
        var hasGold = TryReadIntMember(instance, new[] { "Gold" }, out var gold);
        var hasIron = TryReadIntMember(instance, new[] { "Iron" }, out var iron);
        var hasPopulationCap = TryReadIntMember(instance, new[] { "PopulationCap", "PopCap", "Population" }, out var populationCap);

        if (hasGold && hasIron && hasPopulationCap)
        {
            state = new ResourceState(gold, iron, populationCap);
            return true;
        }

        state = default;
        return false;
    }

    private static bool TryReadIntMember(object instance, string[] names, out int value)
    {
        var properties = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var name in names)
        {
            var property = properties.FirstOrDefault(prop => string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase));
            if (property?.PropertyType == typeof(int))
            {
                value = (int)property.GetValue(instance)!;
                return true;
            }
        }

        value = default;
        return false;
    }

    private readonly record struct ResourceState(int Gold, int Iron, int PopulationCap);

    private readonly record struct ImportResult(bool Accepted, string? FailureReason);
}
