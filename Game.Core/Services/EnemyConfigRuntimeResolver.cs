using System.Text.Json;

namespace Game.Core.Services;

public sealed record EnemyRuntimeStats(
    string EnemyId,
    decimal Health,
    decimal Damage,
    decimal Speed,
    decimal AttackRange,
    int AttackIntervalMs,
    bool IsElite,
    bool IsBoss);

public sealed class EnemyConfigRuntimeResolver
{
    public IReadOnlyList<EnemyRuntimeStats> Resolve(ConfigManager manager, string configJson)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(configJson);
        var activeConfigJson = manager.ActiveConfigJson;
        var sourceJson = string.IsNullOrWhiteSpace(activeConfigJson) ? configJson : activeConfigJson;
        using var document = JsonDocument.Parse(sourceJson);
        return Resolve(manager, document.RootElement);
    }

    public IReadOnlyList<EnemyRuntimeStats> Resolve(ConfigManager manager, JsonDocument configDocument)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(configDocument);
        return Resolve(manager, configDocument.RootElement);
    }

    public IReadOnlyList<EnemyRuntimeStats> Resolve(ConfigManager manager, JsonElement configRoot)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var stats = new List<EnemyRuntimeStats>();
        if (!TryGetPropertyIgnoreCase(configRoot, "enemies", out var enemies) || enemies.ValueKind != JsonValueKind.Array)
        {
            return stats;
        }

        foreach (var item in enemies.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var enemyId = ReadOptionalString(item, "enemy_id") ?? ReadOptionalString(item, "id");
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                continue;
            }

            if (!TryReadDecimalAny(item, new[] { "health", "hp" }, out var health)
                || !TryReadDecimalAny(item, new[] { "damage", "dmg" }, out var damage)
                || !TryReadDecimalAny(item, new[] { "speed", "move_speed" }, out var speed))
            {
                continue;
            }

            var attackRange = ReadDecimalOrDefault(item, defaultValue: 0m, "range", "attack_range");
            var attackIntervalMs = ReadIntOrDefault(item, defaultValue: 2000, "attack_interval", "attack_interval_ms");
            var isElite = ReadBoolOrDefault(item, defaultValue: false, "is_elite");
            var isBoss = ReadBoolOrDefault(item, defaultValue: false, "is_boss");

            stats.Add(new EnemyRuntimeStats(enemyId, health, damage, speed, attackRange, attackIntervalMs, isElite, isBoss));
        }

        return stats;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static bool TryReadDecimal(JsonElement root, string propertyName, out decimal value)
    {
        value = 0m;
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out value))
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var fromDouble))
        {
            value = Convert.ToDecimal(fromDouble);
            return true;
        }

        return false;
    }

    private static bool TryReadDecimalAny(JsonElement root, IReadOnlyList<string> propertyNames, out decimal value)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryReadDecimal(root, propertyName, out value))
            {
                return true;
            }
        }

        value = 0m;
        return false;
    }

    private static decimal ReadDecimalOrDefault(JsonElement root, decimal defaultValue, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryReadDecimal(root, propertyName, out var value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    private static int ReadIntOrDefault(JsonElement root, int defaultValue, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var doubleValue))
            {
                return Convert.ToInt32(Math.Round(doubleValue, MidpointRounding.AwayFromZero));
            }
        }

        return defaultValue;
    }

    private static bool ReadBoolOrDefault(JsonElement root, bool defaultValue, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return defaultValue;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
