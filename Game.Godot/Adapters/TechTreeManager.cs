using System.Collections.Generic;
using System.Text.Json;
using Godot;
using GArray = Godot.Collections.Array;
using GDictionary = Godot.Collections.Dictionary;

namespace Game.Godot.Adapters;

public partial class TechTreeManager : Node
{
    public GDictionary LoadTechTreeFromJsonResource(Variant resourcePath)
    {
        if (resourcePath.VariantType != Variant.Type.String)
        {
            return Fail("TECHTREE_RESOURCE_PATH_REQUIRED");
        }

        var path = resourcePath.AsString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return Fail("TECHTREE_RESOURCE_PATH_REQUIRED");
        }

        if (!FileAccess.FileExists(path))
        {
            return Fail("TECHTREE_RESOURCE_NOT_FOUND");
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            return Fail("TECHTREE_RESOURCE_NOT_FOUND");
        }

        var json = file.GetAsText();
        return ParseRuntime(json);
    }

    private static GDictionary ParseRuntime(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var nodeIds = new GArray();
            var prerequisites = new GDictionary();
            var modifiers = new GDictionary();

            if (root.TryGetProperty("nodes", out var nodesElement) && nodesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var nodeElement in nodesElement.EnumerateArray())
                {
                    if (!TryReadNodeId(nodeElement, out var nodeId))
                    {
                        continue;
                    }

                    nodeIds.Add(nodeId);
                    prerequisites[nodeId] = ReadPrerequisites(nodeElement);
                    modifiers[nodeId] = ReadModifiers(nodeElement);
                }
            }

            return new GDictionary
            {
                ["ok"] = true,
                ["runtime"] = new GDictionary
                {
                    ["node_ids"] = nodeIds,
                    ["prerequisites"] = prerequisites,
                    ["modifiers"] = modifiers,
                },
            };
        }
        catch (JsonException)
        {
            return Fail("TECHTREE_INVALID_JSON");
        }
    }

    private static bool TryReadNodeId(JsonElement nodeElement, out string nodeId)
    {
        nodeId = string.Empty;
        if (!nodeElement.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        nodeId = idElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(nodeId);
    }

    private static GArray ReadPrerequisites(JsonElement nodeElement)
    {
        var result = new GArray();
        if (!nodeElement.TryGetProperty("prerequisites", out var prerequisitesElement) ||
            prerequisitesElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in prerequisitesElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var prerequisite = item.GetString();
                if (!string.IsNullOrWhiteSpace(prerequisite))
                {
                    result.Add(prerequisite.Trim());
                }
            }
        }

        return result;
    }

    private static GDictionary ReadModifiers(JsonElement nodeElement)
    {
        var result = new GDictionary();
        if (!nodeElement.TryGetProperty("modifiers", out var modifiersElement) ||
            modifiersElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in modifiersElement.EnumerateObject())
        {
            if (TryReadNumber(property.Value, out var numeric))
            {
                result[property.Name] = numeric;
            }
        }

        return result;
    }

    private static bool TryReadNumber(JsonElement element, out Variant numeric)
    {
        if (element.TryGetInt64(out var integer))
        {
            numeric = (int)integer;
            return true;
        }

        if (element.TryGetDouble(out var floating))
        {
            numeric = floating;
            return true;
        }

        numeric = default;
        return false;
    }

    private static GDictionary Fail(string errorCode)
    {
        return new GDictionary
        {
            ["ok"] = false,
            ["error_code"] = errorCode,
        };
    }
}
