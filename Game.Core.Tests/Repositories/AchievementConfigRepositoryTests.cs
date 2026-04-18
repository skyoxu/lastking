using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Repositories;

public sealed class AchievementConfigRepositoryTests
{
    // ACC:T27.4
    [Fact]
    public void ShouldLoadAchievementDefinitionsWithAllRequiredFields_WhenStartupConfigurationIsValid()
    {
        var configPath = CreateTempJsonFile("""
        [
          { "id": "first_win", "name": "First Win", "description": "Win one battle.", "unlockCondition": "wins >= 1" },
          { "id": "collector", "name": "Collector", "description": "Collect 10 relics.", "unlockCondition": "relics >= 10" }
        ]
        """);

        try
        {
            var repository = CreateRepositoryInstance();
            var loadedDefinitions = LoadDefinitions(repository, configPath);

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
    public void ShouldRefuseHardcodedOnlyDataSource_WhenExternalConfigurationIsUnavailable()
    {
        var missingConfigPath = BuildRelativeTempPath($"missing-config-{Guid.NewGuid():N}.json");

        Action act = () =>
        {
            var repository = CreateRepositoryInstance();
            _ = LoadDefinitions(repository, missingConfigPath);
        };

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*external configuration*");
    }

    // ACC:T27.12
    [Fact]
    public void ShouldRejectAbsolutePathConfiguration_WhenLoadingDefinitions()
    {
        var repository = CreateRepositoryInstance();
        var absolutePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        Action act = () => _ = LoadDefinitions(repository, absolutePath);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*absolute path*");
    }

    // ACC:T27.12
    [Fact]
    public void ShouldRejectPathTraversalConfiguration_WhenLoadingDefinitions()
    {
        var repository = CreateRepositoryInstance();
        var traversalPath = "..\\..\\outside-config.json";

        Action act = () => _ = LoadDefinitions(repository, traversalPath);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*path traversal*");
    }

    private static object CreateRepositoryInstance()
    {
        var repositoryType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type => type.FullName == "Game.Core.Repositories.AchievementConfigRepository");

        if (repositoryType is null)
        {
            throw new InvalidOperationException("AchievementConfigRepository type was not found.");
        }

        var constructor = repositoryType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw new InvalidOperationException("AchievementConfigRepository must provide a public parameterless constructor.");
        }

        return constructor.Invoke(null);
    }

    private static IReadOnlyList<AchievementDefinitionSnapshot> LoadDefinitions(object repository, string configPath)
    {
        var repositoryType = repository.GetType();
        var loadMethod = repositoryType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method =>
                method.Name is "LoadDefinitions" or "LoadFromFile" or "Load" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(string));

        if (loadMethod is null)
        {
            throw new InvalidOperationException("No public load method accepting a single string path was found.");
        }

        object? result;
        try
        {
            result = loadMethod.Invoke(repository, new object[] { configPath });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        if (result is null)
        {
            throw new InvalidOperationException("Load method returned null.");
        }

        if (result is not IEnumerable enumerable)
        {
            throw new InvalidOperationException("Load method must return an enumerable of achievement definitions.");
        }

        var snapshots = new List<AchievementDefinitionSnapshot>();
        foreach (var item in enumerable)
        {
            if (item is null)
            {
                continue;
            }

            snapshots.Add(new AchievementDefinitionSnapshot(
                ReadStringProperty(item, "Id"),
                ReadStringProperty(item, "Name"),
                ReadStringProperty(item, "Description"),
                ReadStringProperty(item, "UnlockCondition")));
        }

        return snapshots;
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

    private static string ReadStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
        {
            throw new InvalidOperationException($"Required property '{propertyName}' was not found on loaded definition type.");
        }

        return property.GetValue(instance)?.ToString() ?? string.Empty;
    }

    private static string CreateTempJsonFile(string json)
    {
        var filePath = BuildRelativeTempPath($"{Guid.NewGuid():N}.json");
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, json);
        return filePath;
    }

    private static string BuildRelativeTempPath(string fileName)
    {
        return Path.Combine("logs", "tmp", fileName);
    }

    private sealed record AchievementDefinitionSnapshot(
        string Id,
        string Name,
        string Description,
        string UnlockCondition);
}
