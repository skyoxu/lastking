using System.IO;
using System.Text.Json;
using System;

namespace Game.Core.Services;

public sealed class PerformanceGateArtifactContractService
{
    private static readonly string[] RequiredIntegerFields =
    {
        "baseline_threshold_fps",
        "low_1_percent_threshold_fps",
        "average_threshold_fps"
    };

    private static readonly string[] RequiredVerdictFields =
    {
        "baseline_verdict",
        "low_1_percent_verdict",
        "average_verdict"
    };

    public bool IsAllowedArtifactPath(string repoRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(repoRoot) || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        var normalizedRelative = relativePath.Replace('\\', '/').TrimStart('/');
        var allowedPrefix =
            normalizedRelative.StartsWith("logs/perf/", StringComparison.OrdinalIgnoreCase) ||
            normalizedRelative.StartsWith("logs/ci/", StringComparison.OrdinalIgnoreCase);
        if (!allowedPrefix)
        {
            return false;
        }

        var repoRootFull = Path.GetFullPath(repoRoot);
        var candidateFull = Path.GetFullPath(Path.Combine(
            repoRootFull,
            normalizedRelative.Replace('/', Path.DirectorySeparatorChar)));
        var repoPrefix = repoRootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return candidateFull.StartsWith(repoPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsArtifactContractValidFromFile(string artifactPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath) || !File.Exists(artifactPath))
        {
            return false;
        }

        return IsArtifactContractValid(File.ReadAllText(artifactPath));
    }

    public bool IsArtifactContractValid(string artifactJson)
    {
        if (string.IsNullOrWhiteSpace(artifactJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(artifactJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!root.TryGetProperty("runs", out var runs) || runs.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!runs.TryGetProperty("windows_headless", out var headless))
            {
                return false;
            }

            if (!runs.TryGetProperty("windows_playable", out var playable))
            {
                return false;
            }

            return IsRunContractValid(headless) && IsRunContractValid(playable);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsRunContractValid(JsonElement runPayload)
    {
        if (runPayload.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var fieldName in RequiredIntegerFields)
        {
            if (!runPayload.TryGetProperty(fieldName, out var value) || !value.TryGetInt32(out _))
            {
                return false;
            }
        }

        foreach (var fieldName in RequiredVerdictFields)
        {
            if (!runPayload.TryGetProperty(fieldName, out var value) || value.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(value.GetString()))
            {
                return false;
            }
        }

        return true;
    }
}
