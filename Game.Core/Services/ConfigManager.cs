using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Core.Ports;

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
    public const string GovernanceMetadataMissingReason = "CFG_GOVERNANCE_METADATA_MISSING";
    public const string GovernancePrerequisiteMissingReason = "CFG_GOVERNANCE_PREREQUISITE_MISSING";
    public const string GovernancePrerequisiteInvalidReason = "CFG_GOVERNANCE_PREREQUISITE_INVALID";
    public const string GovernancePromotionBlockedReason = "CFG_GOVERNANCE_PROMOTION_BLOCKED";
    public const string VersionMismatchReason = "CFG_VERSION_MISMATCH";
    public const string VersionMigrationRequiredReason = "CFG_VERSION_MIGRATION_REQUIRED";
    public const string MigrationFatalReason = "CFG_MIGRATION_FATAL";

    private BalanceSnapshot _snapshot;
    private bool _hasLoaded;
    private bool _governanceRequiredForReload;
    private string _activeConfigJson;
    private readonly ILogger? _logger;

    public ConfigManager(ILogger? logger = null)
    {
        _logger = logger;
        _snapshot = BalanceSnapshot.Default;
        _hasLoaded = false;
        _governanceRequiredForReload = false;
        _activeConfigJson = string.Empty;
    }

    public BalanceSnapshot Snapshot => _snapshot;
    public string ActiveConfigJson => _activeConfigJson;

    public ConfigLoadResult LoadInitialFromJson(string json, string sourcePath)
    {
        var preferGovernance = ContainsGovernanceSection(json);
        return Apply(json, sourcePath, isReload: false, preferGovernanceValidation: preferGovernance);
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

        return Apply(json, sourcePath, isReload: true, preferGovernanceValidation: false);
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

    private ConfigLoadResult Apply(string json, string sourcePath, bool isReload, bool preferGovernanceValidation)
    {
        var requireGovernanceMetadata = preferGovernanceValidation || (isReload && _governanceRequiredForReload);
        if (!TryParse(
                json,
                requireGovernanceMetadata,
                out var parsed,
                out var reasons,
                out var parsedHasGovernanceMetadata))
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
        _governanceRequiredForReload = _governanceRequiredForReload || parsedHasGovernanceMetadata;
        _activeConfigJson = json;

        return new ConfigLoadResult(
            Accepted: true,
            Source: resolvedSource,
            SourcePath: sourcePath,
            ConfigHash: ComputeHash(json),
            Snapshot: parsed,
            ReasonCodes: Array.Empty<string>());
    }

    private bool TryParse(
        string json,
        bool requireGovernanceMetadata,
        out BalanceSnapshot snapshot,
        out IReadOnlyList<string> reasonCodes,
        out bool hasGovernanceMetadata)
    {
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        hasGovernanceMetadata = false;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            snapshot = BalanceSnapshot.Default;
            reasonCodes = new[] { ParseErrorReason };
            hasGovernanceMetadata = false;
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

            if (TryEvaluateVersionPolicy(root, out var versionPolicyReasons, out var versionPolicyLogMessage))
            {
                if (!string.IsNullOrWhiteSpace(versionPolicyLogMessage))
                {
                    _logger?.Error(versionPolicyLogMessage);
                }
                snapshot = BalanceSnapshot.Default;
                reasonCodes = versionPolicyReasons;
                return false;
            }

            var daySeconds = ReadRequiredInt(root, "time.day_seconds", minimum: 1, reasons);
            var nightSeconds = ReadRequiredInt(root, "time.night_seconds", minimum: 1, reasons);
            var day1Budget = ReadRequiredInt(root, "waves.normal.day1_budget", minimum: 0, reasons);
            var dailyGrowth = ReadRequiredDecimal(root, "waves.normal.daily_growth", minimumExclusive: 0m, reasons);
            var eliteChannel = ReadRequiredString(root, "channels.elite", reasons);
            var bossChannel = ReadRequiredString(root, "channels.boss", reasons);

            var spawnCadence = ReadOptionalInt(root, "spawn.cadence_seconds", defaultValue: 10, minimum: 1, reasons);
            var regularCadence = ReadOptionalInt(root, "spawn.profiles.regular.cadence_seconds", defaultValue: spawnCadence, minimum: 1, reasons);
            var bossCadence = ReadOptionalInt(root, "spawn.profiles.boss.cadence_seconds", defaultValue: spawnCadence, minimum: 1, reasons);
            var bossCount = ReadOptionalInt(root, "boss.count", defaultValue: 2, minimum: 1, reasons);
            var castleStartHp = ReadOptionalInt(root, "battle.castle_start_hp", defaultValue: 100, minimum: 1, reasons);
            var eliteRule = ReadOptionalChannelRule(root, "waves.elite", defaultRule: new ChannelRule(120, 1.2m, 8, 20), reasons);
            var bossRule = ReadOptionalChannelRule(root, "waves.boss", defaultRule: new ChannelRule(300, 1.2m, 3, 100), reasons);
            if (TryResolvePath(root, "governance", out var governance) && governance.ValueKind == JsonValueKind.Object)
            {
                hasGovernanceMetadata = true;
                ValidateGovernance(governance, reasons);
            }
            else if (requireGovernanceMetadata)
            {
                reasons.Add(GovernanceMetadataMissingReason);
                reasons.Add(GovernancePromotionBlockedReason);
            }
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
                RegularSpawnCadenceSeconds: regularCadence,
                BossSpawnCadenceSeconds: bossCadence,
                BossCount: bossCount,
                CastleStartHp: castleStartHp,
                EliteRule: eliteRule,
                BossRule: bossRule)
                .WithEconomyRules(economyRules);

            reasonCodes = Array.Empty<string>();
            return true;
        }
    }

    private static ChannelRule ReadOptionalChannelRule(
        JsonElement root,
        string path,
        ChannelRule defaultRule,
        ISet<string> reasons)
    {
        if (!TryResolvePath(root, path, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return defaultRule;
        }

        var day1Budget = ReadOptionalInt(value, "day1_budget", defaultValue: defaultRule.Day1Budget, minimum: 0, reasons);
        var dailyGrowth = ReadOptionalDecimal(value, "daily_growth", defaultValue: defaultRule.DailyGrowth, minimumExclusive: 0m, reasons);
        var channelLimit = ReadOptionalInt(value, "channel_limit", defaultValue: defaultRule.ChannelLimit, minimum: 1, reasons);
        var costPerEnemy = ReadOptionalInt(value, "cost_per_enemy", defaultValue: defaultRule.CostPerEnemy, minimum: 1, reasons);
        return new ChannelRule(day1Budget, dailyGrowth, channelLimit, costPerEnemy);
    }

    private static decimal ReadOptionalDecimal(JsonElement root, string path, decimal defaultValue, decimal minimumExclusive, ISet<string> reasons)
    {
        if (!TryResolvePath(root, path, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            reasons.Add(InvalidTypeReason);
            return defaultValue;
        }

        var raw = value.GetRawText();
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            reasons.Add(InvalidTypeReason);
            return defaultValue;
        }

        if (parsed <= minimumExclusive)
        {
            reasons.Add(OutOfRangeReason);
            return defaultValue;
        }

        return parsed;
    }

    private static void ValidateGovernance(JsonElement governance, ISet<string> reasons)
    {
        var schemaVersion = ReadOptionalString(governance, "schema_version");
        var tuningSetId = ReadOptionalString(governance, "tuning_set_id");
        if (string.IsNullOrWhiteSpace(schemaVersion) || string.IsNullOrWhiteSpace(tuningSetId))
        {
            reasons.Add(GovernanceMetadataMissingReason);
            reasons.Add(GovernancePromotionBlockedReason);
        }

        if (!TryResolvePath(governance, "promotion", out var promotion) || promotion.ValueKind != JsonValueKind.Object)
        {
            reasons.Add(GovernancePrerequisiteMissingReason);
            reasons.Add(GovernancePromotionBlockedReason);
            return;
        }

        var approvalTicket = ReadOptionalString(promotion, "approval_ticket");
        var soakReportId = ReadOptionalString(promotion, "soak_report_id");
        if (string.IsNullOrWhiteSpace(approvalTicket) || string.IsNullOrWhiteSpace(soakReportId))
        {
            reasons.Add(GovernancePrerequisiteMissingReason);
            reasons.Add(GovernancePromotionBlockedReason);
        }

        if (TryResolvePath(promotion, "regression_gate_passed", out var regressionGate))
        {
            if (regressionGate.ValueKind != JsonValueKind.True && regressionGate.ValueKind != JsonValueKind.False)
            {
                reasons.Add(InvalidTypeReason);
                reasons.Add(GovernancePromotionBlockedReason);
            }
            else if (!regressionGate.GetBoolean())
            {
                reasons.Add(GovernancePrerequisiteInvalidReason);
                reasons.Add(GovernancePromotionBlockedReason);
            }
        }
        else
        {
            reasons.Add(GovernancePrerequisiteMissingReason);
            reasons.Add(GovernancePromotionBlockedReason);
        }
    }

    private static string ReadOptionalString(JsonElement root, string path)
    {
        if (!TryResolvePath(root, path, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return value.GetString() ?? string.Empty;
    }

    private static bool TryEvaluateVersionPolicy(
        JsonElement root,
        out IReadOnlyList<string> reasonCodes,
        out string logMessage)
    {
        reasonCodes = Array.Empty<string>();
        logMessage = string.Empty;

        if (!TryResolvePath(root, "version", out var versionElement) ||
            versionElement.ValueKind != JsonValueKind.String ||
            !TryResolvePath(root, "expectedVersion", out var expectedVersionElement) ||
            expectedVersionElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var version = versionElement.GetString() ?? string.Empty;
        var expectedVersion = expectedVersionElement.GetString() ?? string.Empty;
        if (string.Equals(version, expectedVersion, StringComparison.Ordinal))
        {
            return false;
        }

        var reasons = new List<string>
        {
            VersionMismatchReason,
            VersionMigrationRequiredReason
        };
        logMessage =
            $"Config version mismatch; migration required. version='{version}', expectedVersion='{expectedVersion}'.";

        var forceMigration = TryResolvePath(root, "forceMigration", out var forceMigrationElement) &&
                             forceMigrationElement.ValueKind == JsonValueKind.True;

        if (!forceMigration || !TryResolvePath(root, "migration", out var migration) || migration.ValueKind != JsonValueKind.Object)
        {
            reasonCodes = reasons;
            return true;
        }

        var migrationStatus = ReadOptionalString(migration, "status");
        var migrationRecoverable = TryResolvePath(migration, "recoverable", out var recoverableElement) &&
                                   recoverableElement.ValueKind == JsonValueKind.True;

        if (string.Equals(migrationStatus, "failed", StringComparison.OrdinalIgnoreCase) && !migrationRecoverable)
        {
            var errorCode = ReadOptionalString(migration, "errorCode");
            var resolvedErrorCode = string.IsNullOrWhiteSpace(errorCode) ? MigrationFatalReason : errorCode;
            reasons.Add(resolvedErrorCode);
            logMessage =
                $"Config migration failed fatally after version mismatch. version='{version}', expectedVersion='{expectedVersion}', errorCode='{resolvedErrorCode}'.";
            reasonCodes = reasons;
            return true;
        }

        if (string.Equals(migrationStatus, "succeeded", StringComparison.OrdinalIgnoreCase) &&
            TryResolvePath(migration, "steps", out var stepsElement) &&
            stepsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in stepsElement.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var isStrict = TryResolvePath(step, "strict", out var strictElement) && strictElement.ValueKind == JsonValueKind.True;
                if (!isStrict)
                {
                    continue;
                }

                var stepName = ReadOptionalString(step, "name");
                if (string.IsNullOrWhiteSpace(stepName))
                {
                    continue;
                }

                var stepReason = $"CFG_MIGRATION_STEP_{stepName.Trim().ToUpperInvariant()}";
                if (!reasons.Contains(stepReason, StringComparer.Ordinal))
                {
                    reasons.Add(stepReason);
                }
            }
        }

        reasonCodes = reasons;
        return true;
    }

    private static bool ContainsGovernanceSection(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("governance", out var governance)
                   && governance.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
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
