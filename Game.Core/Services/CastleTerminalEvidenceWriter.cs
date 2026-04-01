using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Game.Core.Contracts.Lastking;
using Game.Core.State;

namespace Game.Core.Services;

public sealed class CastleTerminalEvidenceWriter
{
    private readonly string allowedRoot;

    public CastleTerminalEvidenceWriter(string allowedRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(allowedRoot);
        this.allowedRoot = NormalizePath(allowedRoot);
    }

    public string Write(
        string directoryPath,
        string runId,
        int dayNumber,
        GameFlowState terminalState,
        int currentHp,
        IReadOnlyList<CastleHpChanged> hpTrace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(hpTrace);

        var safeDirectoryPath = RequireAllowedPath(directoryPath);
        Directory.CreateDirectory(safeDirectoryPath);
        var evidencePath = Path.Combine(safeDirectoryPath, $"{runId}-castle-terminal-evidence.json");
        var payload = new
        {
            run_id = runId,
            day_number = dayNumber,
            terminal_state = terminalState.ToString(),
            current_hp = currentHp,
            hp_trace = hpTrace.Select(change => new
            {
                previous_hp = change.PreviousHp,
                current_hp = change.CurrentHp,
                changed_at = change.ChangedAt.ToString("o")
            })
        };

        File.WriteAllText(evidencePath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        return evidencePath;
    }

    public bool Validate(string evidencePath, string expectedRunId)
    {
        if (string.IsNullOrWhiteSpace(evidencePath) || string.IsNullOrWhiteSpace(expectedRunId))
        {
            return false;
        }

        var safeEvidencePath = RequireAllowedPath(evidencePath);
        if (!File.Exists(safeEvidencePath))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(safeEvidencePath));
        var root = doc.RootElement;
        if (!root.TryGetProperty("run_id", out var runIdElement) || runIdElement.GetString() != expectedRunId)
        {
            return false;
        }

        if (!root.TryGetProperty("terminal_state", out var stateElement) || stateElement.GetString() != GameFlowState.GameOver.ToString())
        {
            return false;
        }

        if (!root.TryGetProperty("hp_trace", out var traceElement) || traceElement.ValueKind != JsonValueKind.Array || traceElement.GetArrayLength() == 0)
        {
            return false;
        }

        var last = traceElement[traceElement.GetArrayLength() - 1];
        return last.TryGetProperty("current_hp", out var currentHpElement) && currentHpElement.GetInt32() == 0;
    }

    private string RequireAllowedPath(string path)
    {
        var normalized = NormalizePath(path);
        if (string.Equals(normalized, allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        if (normalized.StartsWith(allowedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(allowedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        throw new InvalidDataException($"Evidence path must stay under logs root: {allowedRoot}");
    }

    private static string NormalizePath(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }
}
