using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Game.Core.Services;

/// <summary>
/// Single authoritative loader/parser for gameplay balance configuration.
/// </summary>
public sealed class ConfigManager
{
    public const string ParseErrorReason = "CFG_PARSE_ERROR";
    public const string FileNotFoundReason = "CFG_FILE_NOT_FOUND";
    public const string FileUnreadableReason = "CFG_FILE_UNREADABLE";
    public const string SchemaRejectedReason = "CFG_SCHEMA_REJECTED";
    public const string InvalidOrderReason = "CFG_INVALID_LOAD_ORDER";
    public const string MissingKeyReason = "CFG_MISSING_KEY";
    public const string InvalidTypeReason = "CFG_INVALID_TYPE";
    public const string OutOfRangeReason = "CFG_OUT_OF_RANGE";

    private BalanceSnapshot _snapshot;
    private bool _hasLoaded;

    public ConfigManager()
    {
        _snapshot = BalanceSnapshot.Default;
        _hasLoaded = false;
    }

    public BalanceSnapshot Snapshot => _snapshot;

    public ConfigLoadResult LoadInitialFromJson(string json, string sourcePath)
    {
        return Apply(json, sourcePath, isReload: false);
    }

    public ConfigLoadResult ReloadFromJson(string json, string sourcePath)
    {
        if (!_hasLoaded)
        {
            return new ConfigLoadResult(
                Accepted: false,
                Source: "fallback",
                SourcePath: sourcePath,
                ConfigHash: ComputeHash(json),
                Snapshot: BalanceSnapshot.Default,
                ReasonCodes: new[] { InvalidOrderReason });
        }

        return Apply(json, sourcePath, isReload: true);
    }

    public ConfigLoadResult LoadInitialFromFile(string filePath)
    {
        if (Directory.Exists(filePath))
        {
            return new ConfigLoadResult(
                Accepted: false,
                Source: "fallback",
                SourcePath: filePath,
                ConfigHash: string.Empty,
                Snapshot: BalanceSnapshot.Default,
                ReasonCodes: new[] { FileUnreadableReason });
        }

        if (!File.Exists(filePath))
        {
            return new ConfigLoadResult(
                Accepted: false,
                Source: "fallback",
                SourcePath: filePath,
                ConfigHash: string.Empty,
                Snapshot: BalanceSnapshot.Default,
                ReasonCodes: new[] { FileNotFoundReason });
        }

        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            return LoadInitialFromJson(text, filePath);
        }
        catch (Exception)
        {
            return new ConfigLoadResult(
                Accepted: false,
                Source: "fallback",
                SourcePath: filePath,
                ConfigHash: string.Empty,
                Snapshot: BalanceSnapshot.Default,
                ReasonCodes: new[] { FileUnreadableReason });
        }
    }

    private ConfigLoadResult Apply(string json, string sourcePath, bool isReload)
    {
        if (!TryParse(json, out var parsed, out var reasons))
        {
            var fallbackSnapshot = _snapshot;
            var source = "fallback";
            if (!_hasLoaded)
            {
                fallbackSnapshot = BalanceSnapshot.Default;
            }

            return new ConfigLoadResult(
                Accepted: false,
                Source: source,
                SourcePath: sourcePath,
                ConfigHash: ComputeHash(json),
                Snapshot: fallbackSnapshot,
                ReasonCodes: reasons);
        }

        _snapshot = parsed;
        var resolvedSource = isReload && _hasLoaded ? "reload" : "initial";
        _hasLoaded = true;

        return new ConfigLoadResult(
            Accepted: true,
            Source: resolvedSource,
            SourcePath: sourcePath,
            ConfigHash: ComputeHash(json),
            Snapshot: parsed,
            ReasonCodes: Array.Empty<string>());
    }

    private static bool TryParse(string json, out BalanceSnapshot snapshot, out IReadOnlyList<string> reasonCodes)
    {
        var reasons = new HashSet<string>(StringComparer.Ordinal);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            snapshot = BalanceSnapshot.Default;
            reasonCodes = new[] { ParseErrorReason };
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (LooksLikePressureNormalizationPayload(root) &&
                !PressureNormalizationConfigContractValidator.TryValidate(root, out _))
            {
                snapshot = BalanceSnapshot.Default;
                reasonCodes = new[] { SchemaRejectedReason };
                return false;
            }

            var daySeconds = ReadRequiredInt(root, "time.day_seconds", minimum: 1, reasons);
            var nightSeconds = ReadRequiredInt(root, "time.night_seconds", minimum: 1, reasons);
            var day1Budget = ReadRequiredInt(root, "waves.normal.day1_budget", minimum: 0, reasons);
            var dailyGrowth = ReadRequiredDecimal(root, "waves.normal.daily_growth", minimumExclusive: 0m, reasons);
            var eliteChannel = ReadRequiredString(root, "channels.elite", reasons);
            var bossChannel = ReadRequiredString(root, "channels.boss", reasons);

            var spawnCadence = ReadOptionalInt(root, "spawn.cadence_seconds", defaultValue: 10, minimum: 1, reasons);
            var bossCount = ReadOptionalInt(root, "boss.count", defaultValue: 2, minimum: 1, reasons);
            var castleStartHp = ReadOptionalInt(root, "battle.castle_start_hp", defaultValue: 100, minimum: 1, reasons);
            var economyRules = EconomyRulesReader.ReadEconomyRules(
                root,
                reasons,
                InvalidTypeReason,
                OutOfRangeReason);

            if (reasons.Count > 0)
            {
                snapshot = BalanceSnapshot.Default;
                reasonCodes = reasons.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                return false;
            }

            snapshot = new BalanceSnapshot(
                DaySeconds: daySeconds,
                NightSeconds: nightSeconds,
                Day1Budget: day1Budget,
                DailyGrowth: dailyGrowth,
                EliteChannel: eliteChannel,
                BossChannel: bossChannel,
                SpawnCadenceSeconds: spawnCadence,
                BossCount: bossCount,
                CastleStartHp: castleStartHp)
                .WithEconomyRules(economyRules);

            reasonCodes = Array.Empty<string>();
            return true;
        }
    }

    private static int ReadRequiredInt(JsonElement root, string path, int minimum, ISet<string> reasons)
    {
        if (!TryResolvePath(root, path, out var value))
        {
            reasons.Add(MissingKeyReason);
            return 0;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var parsed))
        {
            reasons.Add(InvalidTypeReason);
            return 0;
        }

        if (parsed < minimum)
        {
            reasons.Add(OutOfRangeReason);
        }

        return parsed;
    }

    private static int ReadOptionalInt(JsonElement root, string path, int defaultValue, int minimum, ISet<string> reasons)
    {
        if (!TryResolvePath(root, path, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var parsed))
        {
            reasons.Add(InvalidTypeReason);
            return defaultValue;
        }

        if (parsed < minimum)
        {
            reasons.Add(OutOfRangeReason);
            return defaultValue;
        }

        return parsed;
    }

    private static decimal ReadRequiredDecimal(JsonElement root, string path, decimal minimumExclusive, ISet<string> reasons)
    {
        if (!TryResolvePath(root, path, out var value))
        {
            reasons.Add(MissingKeyReason);
            return 0m;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            reasons.Add(InvalidTypeReason);
            return 0m;
        }

        var raw = value.GetRawText();
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            reasons.Add(InvalidTypeReason);
            return 0m;
        }

        if (parsed <= minimumExclusive)
        {
            reasons.Add(OutOfRangeReason);
        }

        return parsed;
    }

    private static string ReadRequiredString(JsonElement root, string path, ISet<string> reasons)
    {
        if (!TryResolvePath(root, path, out var value))
        {
            reasons.Add(MissingKeyReason);
            return string.Empty;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            reasons.Add(InvalidTypeReason);
            return string.Empty;
        }

        var parsed = value.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(parsed))
        {
            reasons.Add(OutOfRangeReason);
        }

        return parsed;
    }

    private static bool TryResolvePath(JsonElement root, string path, out JsonElement value)
    {
        value = root;
        foreach (var segment in path.Split('.'))
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikePressureNormalizationPayload(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return root.TryGetProperty("baseline", out _) ||
               root.TryGetProperty("min_pressure", out _) ||
               root.TryGetProperty("max_pressure", out _) ||
               root.TryGetProperty("normalization_factors", out _) ||
               root.TryGetProperty("constraints", out _);
    }

    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
