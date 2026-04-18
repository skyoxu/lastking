using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class LocalizationManagerTests
{
    // ACC:T28.6
    [Fact]
    public void ShouldLoadAndResolveTranslations_WhenSupportedLanguageFilesAreProvided()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var enUsFile = WriteJsonResource(
                tempDirectory,
                "en-US",
                new Dictionary<string, string>
                {
                    ["GREETING"] = "HelloEn"
                });

            var zhCnFile = WriteJsonResource(
                tempDirectory,
                "zh-CN",
                new Dictionary<string, string>
                {
                    ["GREETING"] = "HelloZh"
                });

            var harness = CreateHarness(tempDirectory);

            var enLoadResult = harness.TryLoadLanguageResource("en-US", enUsFile);
            var zhLoadResult = harness.TryLoadLanguageResource("zh-CN", zhCnFile);
            var enSwitchResult = harness.TrySwitchLanguage("en-US");
            var enValue = harness.GetText("GREETING");
            var zhSwitchResult = harness.TrySwitchLanguage("zh-CN");
            var zhValue = harness.GetText("GREETING");

            (enLoadResult || enSwitchResult).Should().BeTrue("LocalizationManager must be testable for en-US resource loading.");
            (zhLoadResult || zhSwitchResult).Should().BeTrue("LocalizationManager must be testable for zh-CN resource loading.");
            enSwitchResult.Should().BeTrue("switching to en-US should succeed when resource data is valid.");
            zhSwitchResult.Should().BeTrue("switching to zh-CN should succeed when resource data is valid.");
            enValue.Should().Be("HelloEn");
            zhValue.Should().Be("HelloZh");
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    // ACC:T28.10
    [Fact]
    public void ShouldReturnKeyLiteral_WhenTranslationKeyIsMissing()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var enUsFile = WriteJsonResource(
                tempDirectory,
                "en-US",
                new Dictionary<string, string>
                {
                    ["KNOWN_KEY"] = "KnownValue"
                });

            var zhCnFile = WriteJsonResource(
                tempDirectory,
                "zh-CN",
                new Dictionary<string, string>
                {
                    ["KNOWN_KEY"] = "KnownValueZh"
                });

            var harness = CreateHarness(tempDirectory);
            harness.TryLoadLanguageResource("en-US", enUsFile);
            harness.TryLoadLanguageResource("zh-CN", zhCnFile);

            var switchResult = harness.TrySwitchLanguage("en-US");
            var missingValue = harness.GetText("MISSING_KEY");

            switchResult.Should().BeTrue("en-US should be available before checking fallback behavior.");
            missingValue.Should().Be("MISSING_KEY", "missing keys must fallback to key literal.");
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    // ACC:T28.15
    [Fact]
    public void ShouldRefuseLanguageSwitchAndKeepCurrentLanguage_WhenTargetLanguageResourceIsInvalid()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var zhCnFile = WriteJsonResource(
                tempDirectory,
                "zh-CN",
                new Dictionary<string, string>
                {
                    ["GREETING"] = "HelloZh"
                });

            var enUsInvalidFile = WriteMalformedJsonResource(tempDirectory, "en-US");

            var harness = CreateHarness(tempDirectory);

            harness.TryLoadLanguageResource("zh-CN", zhCnFile);
            var zhSwitchResult = harness.TrySwitchLanguage("zh-CN");
            var enLoadResult = harness.TryLoadLanguageResource("en-US", enUsInvalidFile);
            var enSwitchResult = harness.TrySwitchLanguage("en-US");
            var currentLanguage = harness.GetCurrentLanguage();

            zhSwitchResult.Should().BeTrue("baseline language must be set before refusal check.");
            enLoadResult.Should().BeFalse("malformed language resource must fail to load.");
            enSwitchResult.Should().BeFalse("switch must be refused when loading/parsing fails.");
            currentLanguage.Should().Be("zh-CN", "current language must remain unchanged after refused switch.");
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    public void ShouldExposeBothRequiredCultures_WhenInitializedWithDefaultConfiguration()
    {
        var harness = CreateHarness(resourceRoot: null);

        var enSwitchResult = harness.TrySwitchLanguage("en-US");
        var zhSwitchResult = harness.TrySwitchLanguage("zh-CN");

        enSwitchResult.Should().BeTrue("RED-FIRST: default runtime setup should include en-US locale resource.");
        zhSwitchResult.Should().BeTrue("RED-FIRST: default runtime setup should include zh-CN locale resource.");
    }

    private static LocalizationManagerHarness CreateHarness(string? resourceRoot)
    {
        var localizationManagerType = ResolveLocalizationManagerType();
        var instance = CreateInstance(localizationManagerType, resourceRoot);
        return new LocalizationManagerHarness(instance, localizationManagerType, resourceRoot);
    }

    private static Type ResolveLocalizationManagerType()
    {
        var candidateTypeNames = new[]
        {
            "Game.Core.Services.LocalizationManager",
            "Game.Core.Localization.LocalizationManager",
            "Game.Core.LocalizationManager",
            "LocalizationManager"
        };

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var typeName in candidateTypeNames)
            {
                var candidate = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (candidate != null)
                {
                    return candidate;
                }
            }
        }

        var fallback = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .FirstOrDefault(type => string.Equals(type.Name, "LocalizationManager", StringComparison.Ordinal));

        fallback.Should().NotBeNull("LocalizationManager capability must exist for i18n acceptance.");
        return fallback!;
    }

    private static object CreateInstance(Type localizationManagerType, string? resourceRoot)
    {
        var constructors = localizationManagerType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderBy(ctor => ctor.GetParameters().Length)
            .ToArray();

        foreach (var constructor in constructors)
        {
            var arguments = BuildConstructorArguments(constructor.GetParameters(), resourceRoot);

            try
            {
                var instance = constructor.Invoke(arguments);
                if (instance != null)
                {
                    ConfigureResourceRoot(instance, localizationManagerType, resourceRoot);
                    return instance;
                }
            }
            catch
            {
                // Try next constructor.
            }
        }

        var fallbackInstance = Activator.CreateInstance(localizationManagerType, nonPublic: true);
        fallbackInstance.Should().NotBeNull("LocalizationManager instance must be creatable for acceptance tests.");

        ConfigureResourceRoot(fallbackInstance!, localizationManagerType, resourceRoot);
        return fallbackInstance!;
    }

    private static object?[] BuildConstructorArguments(ParameterInfo[] parameters, string? resourceRoot)
    {
        var arguments = new object?[parameters.Length];

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];

            if (parameter.ParameterType == typeof(string))
            {
                arguments[index] = resourceRoot ?? string.Empty;
                continue;
            }

            if (parameter.ParameterType == typeof(CultureInfo))
            {
                arguments[index] = new CultureInfo("en-US");
                continue;
            }

            if (parameter.HasDefaultValue)
            {
                arguments[index] = parameter.DefaultValue;
                continue;
            }

            arguments[index] = parameter.ParameterType.IsValueType
                ? Activator.CreateInstance(parameter.ParameterType)
                : null;
        }

        return arguments;
    }

    private static void ConfigureResourceRoot(object instance, Type localizationManagerType, string? resourceRoot)
    {
        if (string.IsNullOrWhiteSpace(resourceRoot))
        {
            return;
        }

        var propertyNames = new[]
        {
            "ResourceRootPath",
            "ResourceDirectory",
            "LocalizationRoot",
            "LocalesPath",
            "LocaleDirectory"
        };

        foreach (var propertyName in propertyNames)
        {
            var property = localizationManagerType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.CanWrite == true && property.PropertyType == typeof(string))
            {
                property.SetValue(instance, resourceRoot);
            }
        }

        InvokeBestEffort(instance, localizationManagerType, "Initialize", resourceRoot);
        InvokeBestEffort(instance, localizationManagerType, "Load", resourceRoot);
        InvokeBestEffort(instance, localizationManagerType, "Reload", Array.Empty<object>());
    }

    private static void InvokeBestEffort(object instance, Type type, string methodName, params object[] arguments)
    {
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .ToArray();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != arguments.Length)
            {
                continue;
            }

            try
            {
                method.Invoke(instance, arguments);
                return;
            }
            catch
            {
                // Ignore and try next overload.
            }
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type != null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static string CreateTempDirectory()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "lastking-l10n-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static string WriteJsonResource(string rootDirectory, string culture, IReadOnlyDictionary<string, string> entries)
    {
        var filePath = Path.Combine(rootDirectory, culture + ".json");
        var lines = entries.Select(pair => $"  \"{EscapeJson(pair.Key)}\": \"{EscapeJson(pair.Value)}\"");
        var json = "{\n" + string.Join(",\n", lines) + "\n}";
        File.WriteAllText(filePath, json);
        return filePath;
    }

    private static string WriteMalformedJsonResource(string rootDirectory, string culture)
    {
        var filePath = Path.Combine(rootDirectory, culture + ".json");
        var malformedJson = "{\"GREETING\":\"BrokenValue\"";
        File.WriteAllText(filePath, malformedJson);
        return filePath;
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private sealed class LocalizationManagerHarness
    {
        private readonly object instance;
        private readonly Type type;
        private readonly string? resourceRoot;

        public LocalizationManagerHarness(object instance, Type type, string? resourceRoot)
        {
            this.instance = instance;
            this.type = type;
            this.resourceRoot = resourceRoot;
        }

        public bool TryLoadLanguageResource(string culture, string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var candidateMethods = new[]
            {
                "LoadLanguage",
                "LoadLocale",
                "LoadTranslations",
                "RegisterLanguage",
                "RegisterLocale",
                "AddLanguageResource",
                "AddLocaleResource"
            };

            foreach (var method in GetNamedMethods(candidateMethods))
            {
                var parameters = method.GetParameters();
                if (!TryBuildLoadArguments(parameters, culture, filePath, out var arguments))
                {
                    continue;
                }

                try
                {
                    var result = method.Invoke(this.instance, arguments);
                    return InterpretBooleanResult(method.ReturnType, result, expectedSuccessOnVoid: true);
                }
                catch
                {
                    // Try next API shape.
                }
            }

            if (!string.IsNullOrWhiteSpace(this.resourceRoot))
            {
                InvokeBestEffort(this.instance, this.type, "Load", Array.Empty<object>());
                InvokeBestEffort(this.instance, this.type, "Reload", Array.Empty<object>());
            }

            return false;
        }

        public bool TrySwitchLanguage(string culture)
        {
            var candidateMethods = new[]
            {
                "TrySetLanguage",
                "TrySwitchLanguage",
                "SetLanguage",
                "SwitchLanguage",
                "ChangeLanguage",
                "UseLanguage",
                "SetLocale",
                "SwitchLocale"
            };

            foreach (var method in GetNamedMethods(candidateMethods))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                object? argument;
                if (parameters[0].ParameterType == typeof(string))
                {
                    argument = culture;
                }
                else if (parameters[0].ParameterType == typeof(CultureInfo))
                {
                    argument = new CultureInfo(culture);
                }
                else
                {
                    continue;
                }

                try
                {
                    var result = method.Invoke(this.instance, new[] { argument });

                    if (method.ReturnType == typeof(void))
                    {
                        var currentLanguage = GetCurrentLanguage();
                        return string.Equals(currentLanguage, culture, StringComparison.OrdinalIgnoreCase);
                    }

                    return InterpretBooleanResult(method.ReturnType, result, expectedSuccessOnVoid: false);
                }
                catch
                {
                    // Try next API shape.
                }
            }

            var propertyNames = new[] { "CurrentLanguage", "CurrentLocale", "Language", "Locale" };
            foreach (var propertyName in propertyNames)
            {
                var property = this.type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property?.CanWrite == true && property.PropertyType == typeof(string))
                {
                    try
                    {
                        property.SetValue(this.instance, culture);
                        var currentLanguage = GetCurrentLanguage();
                        return string.Equals(currentLanguage, culture, StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // Continue fallback.
                    }
                }
            }

            return false;
        }

        public string GetText(string key)
        {
            var candidateMethods = new[]
            {
                "GetText",
                "Translate",
                "GetTranslation",
                "Localize",
                "Get",
                "Resolve"
            };

            foreach (var method in GetNamedMethods(candidateMethods))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                try
                {
                    var result = method.Invoke(this.instance, new object[] { key });
                    if (result is string text)
                    {
                        return text;
                    }

                    if (result != null)
                    {
                        return result.ToString() ?? string.Empty;
                    }
                }
                catch
                {
                    // Continue fallback.
                }
            }

            var indexer = this.type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (indexer != null)
            {
                var parameters = indexer.GetIndexParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    try
                    {
                        var result = indexer.GetValue(this.instance, new object[] { key });
                        if (result is string text)
                        {
                            return text;
                        }
                    }
                    catch
                    {
                        // Ignore final fallback.
                    }
                }
            }

            return string.Empty;
        }

        public string GetCurrentLanguage()
        {
            var propertyNames = new[] { "CurrentLanguage", "CurrentLocale", "Language", "Locale", "ActiveLanguage", "ActiveLocale" };
            foreach (var propertyName in propertyNames)
            {
                var property = this.type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null)
                {
                    continue;
                }

                try
                {
                    var value = property.GetValue(this.instance);
                    if (value is string text)
                    {
                        return text;
                    }

                    if (value is CultureInfo cultureInfo)
                    {
                        return cultureInfo.Name;
                    }

                    if (value != null)
                    {
                        return value.ToString() ?? string.Empty;
                    }
                }
                catch
                {
                    // Continue fallback.
                }
            }

            var methodNames = new[] { "GetCurrentLanguage", "GetCurrentLocale", "CurrentLanguageCode" };
            foreach (var method in GetNamedMethods(methodNames))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 0)
                {
                    continue;
                }

                try
                {
                    var value = method.Invoke(this.instance, Array.Empty<object>());
                    if (value is string text)
                    {
                        return text;
                    }

                    if (value is CultureInfo cultureInfo)
                    {
                        return cultureInfo.Name;
                    }
                }
                catch
                {
                    // Continue fallback.
                }
            }

            return string.Empty;
        }

        private IEnumerable<MethodInfo> GetNamedMethods(IEnumerable<string> candidateNames)
        {
            var lookup = candidateNames.ToHashSet(StringComparer.Ordinal);
            return this.type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => lookup.Contains(method.Name));
        }

        private static bool TryBuildLoadArguments(ParameterInfo[] parameters, string culture, string filePath, out object?[] arguments)
        {
            arguments = new object?[parameters.Length];

            if (parameters.Length == 1)
            {
                if (parameters[0].ParameterType == typeof(string))
                {
                    arguments[0] = filePath;
                    return true;
                }

                if (parameters[0].ParameterType == typeof(CultureInfo))
                {
                    arguments[0] = new CultureInfo(culture);
                    return true;
                }

                return false;
            }

            if (parameters.Length == 2)
            {
                for (var index = 0; index < parameters.Length; index++)
                {
                    var parameter = parameters[index];

                    if (parameter.ParameterType == typeof(string))
                    {
                        var parameterName = parameter.Name ?? string.Empty;
                        var pathLike = parameterName.Contains("path", StringComparison.OrdinalIgnoreCase)
                            || parameterName.Contains("file", StringComparison.OrdinalIgnoreCase)
                            || parameterName.Contains("resource", StringComparison.OrdinalIgnoreCase);

                        arguments[index] = pathLike ? filePath : culture;
                        continue;
                    }

                    if (parameter.ParameterType == typeof(CultureInfo))
                    {
                        arguments[index] = new CultureInfo(culture);
                        continue;
                    }

                    if (parameter.HasDefaultValue)
                    {
                        arguments[index] = parameter.DefaultValue;
                        continue;
                    }

                    if (parameter.ParameterType.IsValueType)
                    {
                        arguments[index] = Activator.CreateInstance(parameter.ParameterType);
                        continue;
                    }

                    return false;
                }

                return true;
            }

            return false;
        }

        private static bool InterpretBooleanResult(Type returnType, object? result, bool expectedSuccessOnVoid)
        {
            if (returnType == typeof(void))
            {
                return expectedSuccessOnVoid;
            }

            if (returnType == typeof(bool) && result is bool boolResult)
            {
                return boolResult;
            }

            if (result is bool boxedBool)
            {
                return boxedBool;
            }

            if (result is string text && bool.TryParse(text, out var parsed))
            {
                return parsed;
            }

            return false;
        }
    }
}
