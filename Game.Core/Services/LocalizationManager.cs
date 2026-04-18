using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Game.Core.Services;

public sealed class LocalizationManager
{
    private static readonly HashSet<string> SupportedLocales = new(StringComparer.Ordinal)
    {
        "en-US",
        "zh-CN"
    };

    private readonly Dictionary<string, string> localeResourcePaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> localeTables = new(StringComparer.Ordinal);

    public LocalizationManager(string? resourceRootPath = null)
    {
        ResourceRootPath = resourceRootPath ?? string.Empty;
        CurrentLocale = "en-US";
        TryLoadDefaultLocaleResources();
    }

    public string ResourceRootPath { get; set; } = string.Empty;

    public string CurrentLocale { get; private set; }

    public bool ConfigureLocaleResource(string locale, string filePath)
    {
        if (!SupportedLocales.Contains(locale))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        localeResourcePaths[locale] = filePath;
        return true;
    }

    public bool TryLoadLanguageResource(string locale, string filePath)
    {
        if (!ConfigureLocaleResource(locale, filePath))
        {
            return false;
        }

        if (!TryReadTranslationTable(filePath, out var table))
        {
            return false;
        }

        localeTables[locale] = table;
        return true;
    }

    public bool LoadLanguage(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var locale = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(locale))
        {
            return false;
        }

        return TryLoadLanguageResource(locale, filePath);
    }

    public bool TrySwitchLanguage(string locale)
    {
        return SwitchLocale(locale);
    }

    public bool SwitchLanguage(string locale)
    {
        return SwitchLocale(locale);
    }

    public bool SetLocale(string locale)
    {
        return SwitchLocale(locale);
    }

    public bool SwitchLocale(string locale)
    {
        if (!SupportedLocales.Contains(locale))
        {
            return false;
        }

        if (!localeTables.ContainsKey(locale))
        {
            if (!TryLoadLocaleFromConfiguredPath(locale))
            {
                return false;
            }
        }

        if (!localeTables.ContainsKey(locale))
        {
            return false;
        }

        CurrentLocale = locale;
        return true;
    }

    public string GetText(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        if (localeTables.TryGetValue(CurrentLocale, out var table)
            && table.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return key;
    }

    public string Translate(string key)
    {
        return GetText(key);
    }

    public string GetTranslation(string key)
    {
        return GetText(key);
    }

    public string GetCurrentLocale()
    {
        return CurrentLocale;
    }

    private void TryLoadDefaultLocaleResources()
    {
        if (string.IsNullOrWhiteSpace(ResourceRootPath))
        {
            foreach (var locale in SupportedLocales)
            {
                if (!localeTables.ContainsKey(locale))
                {
                    localeTables[locale] = new Dictionary<string, string>(StringComparer.Ordinal);
                }
            }

            return;
        }

        foreach (var locale in SupportedLocales)
        {
            var candidate = Path.Combine(ResourceRootPath, locale + ".json");
            if (File.Exists(candidate))
            {
                TryLoadLanguageResource(locale, candidate);
            }
        }
    }

    private bool TryLoadLocaleFromConfiguredPath(string locale)
    {
        if (!localeResourcePaths.TryGetValue(locale, out var filePath))
        {
            return false;
        }

        if (!TryReadTranslationTable(filePath, out var table))
        {
            return false;
        }

        localeTables[locale] = table;
        return true;
    }

    private static bool TryReadTranslationTable(string filePath, out IReadOnlyDictionary<string, string> table)
    {
        table = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (parsed == null)
            {
                return false;
            }

            table = new Dictionary<string, string>(parsed, StringComparer.Ordinal);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
