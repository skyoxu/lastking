using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Game.Core.Services;

public sealed class TechTreeManager
{
    private Dictionary<string, TechNode> nodesById = new(StringComparer.Ordinal);
    private HashSet<string> unlockedTechIds = new(StringComparer.Ordinal);
    private TechTreeRuntimeSnapshot? currentSnapshot;
    private string deterministicTrace = string.Empty;
    private long version;

    public TechTreeManager()
    {
    }

    public TechTreeManager(string jsonConfig)
    {
        LoadFromJson(jsonConfig);
    }

    public long Version => version;

    public TechTreeRuntimeSnapshot CurrentSnapshot => currentSnapshot ??= BuildRuntimeSnapshot();

    public TechTreeRuntimeSnapshot RuntimeSnapshot => CurrentSnapshot;

    public TechTreeRuntimeSnapshot Snapshot => CurrentSnapshot;

    public TechTreeRuntimeSnapshot LoadFromJson(string jsonConfig)
    {
        nodesById = ParseNodes(jsonConfig);
        ReconcileUnlockedFlags();
        version++;
        currentSnapshot = BuildRuntimeSnapshot();
        return currentSnapshot;
    }

    public TechTreeRuntimeSnapshot LoadConfig(string jsonConfig)
    {
        return LoadFromJson(jsonConfig);
    }

    public TechTreeRuntimeSnapshot Load(string jsonConfig)
    {
        return LoadFromJson(jsonConfig);
    }

    public TechTreeRuntimeSnapshot ReloadFromJson(string jsonConfig)
    {
        return LoadFromJson(jsonConfig);
    }

    public TechTreeRuntimeSnapshot ReloadConfig(string jsonConfig)
    {
        return LoadFromJson(jsonConfig);
    }

    public TechTreeRuntimeSnapshot Reload(string jsonConfig)
    {
        return LoadFromJson(jsonConfig);
    }

    public void SetUnlockedTechIds(IEnumerable<string> techIds)
    {
        unlockedTechIds = NormalizeTechIdSet(techIds);
        ReconcileUnlockedFlags();
        version++;
        currentSnapshot = BuildRuntimeSnapshot();
    }

    public void SetUnlockedNodes(IEnumerable<string> techIds)
    {
        SetUnlockedTechIds(techIds);
    }

    public void ReplaceUnlockedTechIds(IEnumerable<string> techIds)
    {
        SetUnlockedTechIds(techIds);
    }

    public void Unlock(string techId)
    {
        if (string.IsNullOrWhiteSpace(techId))
        {
            return;
        }

        unlockedTechIds.Add(techId.Trim());
        ReconcileUnlockedFlags();
        version++;
        currentSnapshot = BuildRuntimeSnapshot();
    }

    public void UnlockTech(string techId)
    {
        Unlock(techId);
    }

    public bool IsAvailable(string techId)
    {
        if (string.IsNullOrWhiteSpace(techId))
        {
            return false;
        }

        return nodesById.TryGetValue(techId.Trim(), out var node) && ArePrerequisitesSatisfied(node);
    }

    public bool CanUnlock(string techId)
    {
        return IsAvailable(techId);
    }

    public double GetStatMultiplier(string statKey)
    {
        if (string.IsNullOrWhiteSpace(statKey))
        {
            return 1.0d;
        }

        var normalizedStatKey = statKey.Trim();
        var product = 1.0d;

        foreach (var node in nodesById.Values.OrderBy(static node => node.Id, StringComparer.Ordinal))
        {
            if (!node.IsUnlocked || !ArePrerequisitesSatisfied(node))
            {
                continue;
            }

            if (TryResolveMultiplier(node, normalizedStatKey, out var multiplier))
            {
                product *= multiplier;
            }
        }

        return product;
    }

    public double GetMultiplier(string statKey)
    {
        return GetStatMultiplier(statKey);
    }

    public TechTreeRuntimeSnapshot GetRuntimeSnapshot()
    {
        return CurrentSnapshot;
    }

    public TechTreeRuntimeSnapshot BuildRuntimeSnapshot()
    {
        var nodes = nodesById
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(
                static pair => pair.Key,
                static pair => TechNodeView.From(pair.Value),
                StringComparer.Ordinal);

        var multipliers = BuildMultiplierMap();
        deterministicTrace = BuildTrace(multipliers);
        return new TechTreeRuntimeSnapshot(version, nodes, multipliers);
    }

    public string GetDeterministicTrace()
    {
        return deterministicTrace;
    }

    public string GetTraceArtifact()
    {
        return deterministicTrace;
    }

    private static HashSet<string> NormalizeTechIdSet(IEnumerable<string> techIds)
    {
        var normalized = new HashSet<string>(StringComparer.Ordinal);
        if (techIds is null)
        {
            return normalized;
        }

        foreach (var techId in techIds)
        {
            if (!string.IsNullOrWhiteSpace(techId))
            {
                normalized.Add(techId.Trim());
            }
        }

        return normalized;
    }

    private void ReconcileUnlockedFlags()
    {
        foreach (var node in nodesById.Values)
        {
            node.IsUnlocked = unlockedTechIds.Contains(node.Id);
        }
    }

    private bool ArePrerequisitesSatisfied(TechNode node)
    {
        return node.Prerequisites.All(prerequisite => unlockedTechIds.Contains(prerequisite));
    }

    private Dictionary<string, double> BuildMultiplierMap()
    {
        var stats = nodesById.Values
            .Where(node => node.IsUnlocked && ArePrerequisitesSatisfied(node))
            .SelectMany(static node => node.Modifiers.Keys)
            .Distinct(StringComparer.Ordinal);

        var multiplierMap = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var stat in stats)
        {
            multiplierMap[stat] = GetStatMultiplier(stat);
        }

        return multiplierMap;
    }

    private string BuildTrace(IReadOnlyDictionary<string, double> multipliers)
    {
        var unlocked = nodesById.Values
            .Where(static node => node.IsUnlocked)
            .Select(static node => node.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        var multiplierText = multipliers
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{pair.Value.ToString("0.########", CultureInfo.InvariantCulture)}");

        return $"unlocked=[{string.Join(",", unlocked)}];multipliers=[{string.Join(",", multiplierText)}]";
    }

    private static bool TryResolveMultiplier(TechNode node, string statKey, out double multiplier)
    {
        if (node.Modifiers.TryGetValue(statKey, out var configuredMultiplier))
        {
            if (IsSupportedMultiplier(configuredMultiplier))
            {
                multiplier = configuredMultiplier;
                return true;
            }

            multiplier = 1.0d;
            return false;
        }

        if (string.Equals(node.Stat, statKey, StringComparison.Ordinal) && IsSupportedMultiplier(node.Multiplier))
        {
            multiplier = node.Multiplier;
            return true;
        }

        multiplier = 1.0d;
        return false;
    }

    private static bool IsSupportedMultiplier(double multiplier)
    {
        return multiplier > 0.0d && multiplier <= 2.0d;
    }

    private static Dictionary<string, TechNode> ParseNodes(string jsonConfig)
    {
        var result = new Dictionary<string, TechNode>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(jsonConfig))
        {
            return result;
        }

        using var document = JsonDocument.Parse(jsonConfig);
        if (!document.RootElement.TryGetProperty("nodes", out var nodesElement) || nodesElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            if (!nodeElement.TryGetProperty("id", out var idElement))
            {
                continue;
            }

            var nodeId = idElement.GetString();
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                continue;
            }

            var prerequisites = ReadPrerequisites(nodeElement);
            var modifiers = ReadModifiers(nodeElement);

            string stat = string.Empty;
            var multiplier = 1.0d;
            if (nodeElement.TryGetProperty("stat", out var statElement) && statElement.ValueKind == JsonValueKind.String)
            {
                stat = statElement.GetString() ?? string.Empty;
            }

            if (nodeElement.TryGetProperty("multiplier", out var multiplierElement) &&
                multiplierElement.TryGetDouble(out var parsedMultiplier))
            {
                multiplier = parsedMultiplier;
            }

            if (!string.IsNullOrWhiteSpace(stat) &&
                IsSupportedMultiplier(multiplier) &&
                !modifiers.ContainsKey(stat))
            {
                modifiers[stat] = multiplier;
            }

            result[nodeId] = new TechNode
            {
                Id = nodeId,
                Stat = stat,
                Multiplier = multiplier,
                Prerequisites = prerequisites,
                Modifiers = modifiers,
                IsUnlocked = false,
            };
        }

        return result;
    }

    private static List<string> ReadPrerequisites(JsonElement nodeElement)
    {
        if (!nodeElement.TryGetProperty("prerequisites", out var prerequisitesElement) ||
            prerequisitesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return prerequisitesElement
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static Dictionary<string, double> ReadModifiers(JsonElement nodeElement)
    {
        var modifiers = new Dictionary<string, double>(StringComparer.Ordinal);
        if (!nodeElement.TryGetProperty("modifiers", out var modifiersElement) ||
            modifiersElement.ValueKind != JsonValueKind.Object)
        {
            return modifiers;
        }

        foreach (var property in modifiersElement.EnumerateObject())
        {
            if (property.Value.TryGetDouble(out var value) && IsSupportedMultiplier(value))
            {
                modifiers[property.Name] = value;
            }
        }

        return modifiers;
    }
}

public sealed class TechNode
{
    public string Id { get; set; } = string.Empty;

    public string Stat { get; set; } = string.Empty;

    public double Multiplier { get; set; } = 1.0d;

    public bool IsUnlocked { get; set; }

    public List<string> Prerequisites { get; set; } = [];

    public Dictionary<string, double> Modifiers { get; set; } = new(StringComparer.Ordinal);
}

public sealed class TechNodeView
{
    public string Id { get; init; } = string.Empty;

    public IReadOnlyList<string> Prerequisites { get; init; } = [];

    public IReadOnlyDictionary<string, double> Modifiers { get; init; } = new Dictionary<string, double>(StringComparer.Ordinal);

    public static TechNodeView From(TechNode node)
    {
        return new TechNodeView
        {
            Id = node.Id,
            Prerequisites = node.Prerequisites.ToArray(),
            Modifiers = new Dictionary<string, double>(node.Modifiers, StringComparer.Ordinal),
        };
    }
}

public sealed class TechTreeRuntimeSnapshot
{
    private readonly IReadOnlyDictionary<string, double> multipliersByStat;

    public TechTreeRuntimeSnapshot(
        long version,
        IReadOnlyDictionary<string, TechNodeView> nodes,
        IReadOnlyDictionary<string, double> multipliersByStat)
    {
        Version = version;
        Nodes = nodes;
        this.multipliersByStat = multipliersByStat;
    }

    public long Version { get; }

    public IReadOnlyDictionary<string, TechNodeView> Nodes { get; }

    public double GetStatMultiplier(string statKey)
    {
        if (string.IsNullOrWhiteSpace(statKey))
        {
            return 1.0d;
        }

        return multipliersByStat.TryGetValue(statKey.Trim(), out var multiplier)
            ? multiplier
            : 1.0d;
    }

    public double GetMultiplier(string statKey)
    {
        return GetStatMultiplier(statKey);
    }
}
