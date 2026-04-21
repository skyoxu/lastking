using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Game.Core.Services;

public enum BattleReportMetadataMode
{
    Hash,
    Version,
}

public sealed record BattleMatchResult(string Outcome, int WavesSurvived, int EnemiesDefeated, int Score);

public sealed record BattleConfigSnapshot(byte[] Payload, string? DeclaredVersion, BattleMatchResult MatchResult);

public sealed record BattleReportPayload(BattleMatchResult MatchResult, Dictionary<string, string> Metadata);

public sealed class BattleReportFactory
{
    public BattleReportPayload CreateReport(
        int[] trace,
        string selectedMetadataValue,
        string selectedMetadataAlias,
        BattleReportMetadataMode metadataMode = BattleReportMetadataMode.Hash)
    {
        if (trace is null)
        {
            throw new ArgumentNullException(nameof(trace));
        }

        var mode = ResolveMode(selectedMetadataAlias, metadataMode);
        var matchResult = BuildMatchResult(trace);
        var metadata = BuildMetadata(mode, selectedMetadataValue);
        return new BattleReportPayload(matchResult, metadata);
    }

    public BattleReportPayload Generate(BattleConfigSnapshot snapshot, BattleReportMetadataMode metadataMode)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var mode = metadataMode;
        var value = mode switch
        {
            BattleReportMetadataMode.Hash => ComputeSha256Hex(snapshot.Payload),
            BattleReportMetadataMode.Version => ReadVersion(snapshot.DeclaredVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(metadataMode), metadataMode, "Unsupported metadata mode."),
        };

        var metadata = BuildMetadata(mode, value);
        return new BattleReportPayload(snapshot.MatchResult, metadata);
    }

    private static BattleReportMetadataMode ResolveMode(string selectedMetadataAlias, BattleReportMetadataMode fallback)
    {
        var alias = (selectedMetadataAlias ?? string.Empty).Trim();
        if (alias.Equals("config_hash", StringComparison.OrdinalIgnoreCase))
        {
            return BattleReportMetadataMode.Hash;
        }

        if (alias.Equals("config_version", StringComparison.OrdinalIgnoreCase))
        {
            return BattleReportMetadataMode.Version;
        }

        return fallback;
    }

    private static Dictionary<string, string> BuildMetadata(BattleReportMetadataMode mode, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Selected metadata value must be non-empty.", nameof(value));
        }

        var key = mode == BattleReportMetadataMode.Hash ? "config_hash" : "config_version";
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [key] = value,
        };
    }

    private static BattleMatchResult BuildMatchResult(int[] trace)
    {
        var summary = 0;
        for (var index = 0; index < trace.Length; index++)
        {
            summary += trace[index];
        }

        return new BattleMatchResult(
            Outcome: summary >= 0 ? "win" : "loss",
            WavesSurvived: trace.Length,
            EnemiesDefeated: Math.Abs(summary % 1000),
            Score: summary);
    }

    private static string ReadVersion(string? declaredVersion)
    {
        if (string.IsNullOrWhiteSpace(declaredVersion))
        {
            throw new InvalidOperationException("Declared version is required for config_version mode.");
        }

        return declaredVersion.Trim();
    }

    private static string ComputeSha256Hex(byte[] payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
