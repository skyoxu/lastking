using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Game.Core.Repositories;

public sealed class AchievementConfigRepository
{
    public IReadOnlyList<AchievementDefinitionRecord> LoadDefinitions(string configPath)
    {
        var resolvedPath = ResolveAndValidateConfigurationPath(configPath);
        if (!File.Exists(resolvedPath))
        {
            throw new InvalidOperationException("external configuration is required and must exist.");
        }

        var json = File.ReadAllText(resolvedPath);
        var definitions = JsonSerializer.Deserialize<List<AchievementDefinitionRecord>>(json, BuildJsonOptions());
        if (definitions is null || definitions.Count == 0)
        {
            throw new InvalidDataException("external configuration must contain at least one achievement definition.");
        }

        ValidateDefinitions(definitions);
        return definitions;
    }

    private static JsonSerializerOptions BuildJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private static string ResolveAndValidateConfigurationPath(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new InvalidOperationException("external configuration path is required.");
        }

        var normalizedInput = configPath.Trim().Replace('\\', '/');
        var schemePath = normalizedInput;

        if (normalizedInput.Contains("://", StringComparison.Ordinal))
        {
            if (normalizedInput.StartsWith("res://", StringComparison.Ordinal))
            {
                schemePath = normalizedInput["res://".Length..];
            }
            else if (normalizedInput.StartsWith("user://", StringComparison.Ordinal))
            {
                schemePath = Path.Combine("user", normalizedInput["user://".Length..]);
            }
            else
            {
                throw new InvalidOperationException("external configuration path scheme is not allowed.");
            }
        }

        var relativeCandidate = schemePath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relativeCandidate))
        {
            throw new InvalidOperationException("external configuration absolute path is not allowed.");
        }

        if (relativeCandidate
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment == ".."))
        {
            throw new InvalidOperationException("external configuration path traversal is not allowed.");
        }

        var currentRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        var resolved = Path.GetFullPath(relativeCandidate, currentRoot);
        if (!resolved.StartsWith(currentRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("external configuration path traversal is not allowed.");
        }

        return resolved;
    }

    private static void ValidateDefinitions(IEnumerable<AchievementDefinitionRecord> definitions)
    {
        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new InvalidDataException("achievement definition id is required.");
            }

            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                throw new InvalidDataException($"achievement definition '{definition.Id}' missing name.");
            }

            if (string.IsNullOrWhiteSpace(definition.Description))
            {
                throw new InvalidDataException($"achievement definition '{definition.Id}' missing description.");
            }

            if (string.IsNullOrWhiteSpace(definition.UnlockCondition))
            {
                throw new InvalidDataException($"achievement definition '{definition.Id}' missing unlock condition.");
            }
        }

        var duplicateId = definitions
            .GroupBy(definition => definition.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw new InvalidDataException($"duplicate achievement id found: '{duplicateId.Key}'.");
        }
    }
}

public sealed record AchievementDefinitionRecord(
    string Id,
    string Name,
    string Description,
    string UnlockCondition);
