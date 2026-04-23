using System.Text.Json;

namespace Game.Core.Services;

public sealed record EnemyRuntimeStats(
    string EnemyId,
    decimal Health,
    decimal Damage,
    decimal Speed);

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

            if (!TryReadDecimal(item, "health", out var health)
                || !TryReadDecimal(item, "damage", out var damage)
                || !TryReadDecimal(item, "speed", out var speed))
            {
                continue;
            }

            stats.Add(new EnemyRuntimeStats(enemyId, health, damage, speed));
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
