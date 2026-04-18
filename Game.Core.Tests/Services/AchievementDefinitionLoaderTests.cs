using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class AchievementDefinitionLoaderTests
{
    // ACC:T27.4
    [Fact]
    public void ShouldLoadDefinitionsWithAllRequiredFields_WhenStartupUsesExternalConfiguration()
    {
        var configPath = CreateTempJsonFile("""
        [
          { "id": "first_win", "name": "First Win", "description": "Win one battle.", "unlockCondition": "wins >= 1" },
          { "id": "collector", "name": "Collector", "description": "Collect 10 relics.", "unlockCondition": "relics >= 10" }
        ]
        """);

        try
        {
            var loader = CreateLoaderInstance();
            var loadedDefinitions = LoadDefinitionsFromStartup(loader, configPath);

            loadedDefinitions.Should().HaveCount(2);
            loadedDefinitions.Should().OnlyContain(definition =>
                !string.IsNullOrWhiteSpace(definition.Id) &&
                !string.IsNullOrWhiteSpace(definition.Name) &&
                !string.IsNullOrWhiteSpace(definition.Description) &&
                !string.IsNullOrWhiteSpace(definition.UnlockCondition));
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    // ACC:T27.12
    [Fact]
    public void ShouldRefuseHardcodedOnlySource_WhenExternalConfigurationIsUnavailable()
    {
        var missingConfigPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        Action act = () =>
        {
            var loader = CreateLoaderInstance();
            _ = LoadDefinitionsFromStartup(loader, missingConfigPath);
        };

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*external configuration*");
    }

    [Fact]
    public void ShouldRejectDefinitionMissingUnlockCondition_WhenStartupLoadsExternalConfiguration()
    {
        var configPath = CreateTempJsonFile("""
        [
          { "id": "broken", "name": "Broken", "description": "Missing unlock condition." }
        ]
        """);

        try
        {
            Action act = () =>
            {
                var loader = CreateLoaderInstance();
                _ = LoadDefinitionsFromStartup(loader, configPath);
            };

            act.Should()
                .Throw<InvalidDataException>()
                .WithMessage("*unlock condition*");
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    private static object CreateLoaderInstance()
    {
        var loaderType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type =>
                type.FullName == "Game.Core.Services.AchievementDefinitionLoader" ||
                type.Name == "AchievementDefinitionLoader");

        if (loaderType is null)
        {
            throw new InvalidOperationException("AchievementDefinitionLoader type was not found.");
        }

        var constructor = loaderType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw new InvalidOperationException("AchievementDefinitionLoader must provide a public parameterless constructor.");
        }

        return constructor.Invoke(null);
    }

    private static IReadOnlyList<AchievementDefinitionSnapshot> LoadDefinitionsFromStartup(object loader, string configPath)
    {
        var loadMethod = loader.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method =>
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(string) &&
                (method.Name is "LoadAtStartup" or "LoadDefinitionsAtStartup" or "LoadDefinitions" or "Load" ||
                 method.Name.Contains("Load", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(method => method.Name.Equals("LoadAtStartup", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault();

        if (loadMethod is null)
        {
            throw new InvalidOperationException("Loader must expose a public load method accepting an external configuration path.");
        }

        object? rawResult;
        try
        {
            rawResult = loadMethod.Invoke(loader, new object[] { configPath });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        var result = UnwrapTaskResult(rawResult);
        if (result is null)
        {
            throw new InvalidOperationException("Loader returned null when loading achievement definitions.");
        }

        if (result is not IEnumerable enumerable)
        {
            throw new InvalidOperationException("Loader must return an enumerable of achievement definitions.");
        }

        var snapshots = new List<AchievementDefinitionSnapshot>();
        foreach (var item in enumerable)
        {
            if (item is null)
            {
                continue;
            }

            snapshots.Add(new AchievementDefinitionSnapshot(
                ReadStringMember(item, "Id"),
                ReadStringMember(item, "Name"),
                ReadStringMember(item, "Description"),
                ReadStringMember(item, "UnlockCondition")));
        }

        return snapshots;
    }

    private static object? UnwrapTaskResult(object? value)
    {
        if (value is not Task task)
        {
            return value;
        }

        task.GetAwaiter().GetResult();
        var taskType = task.GetType();

        if (!taskType.IsGenericType)
        {
            return null;
        }

        return taskType.GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)?.GetValue(task);
    }

    private static string ReadStringMember(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property is not null)
        {
            return property.GetValue(instance)?.ToString() ?? string.Empty;
        }

        var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (field is not null)
        {
            return field.GetValue(instance)?.ToString() ?? string.Empty;
        }

        throw new InvalidOperationException($"Required member '{name}' was not found on loaded definition type.");
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

    private static string CreateTempJsonFile(string json)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, json);
        return filePath;
    }

    private sealed record AchievementDefinitionSnapshot(
        string Id,
        string Name,
        string Description,
        string UnlockCondition);
}
