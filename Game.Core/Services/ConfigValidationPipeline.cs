using System.Text.Json;

namespace Game.Core.Services;

public sealed class ConfigValidationPipeline
{
    private readonly string? logsCiDirectory;
    private readonly ConfigStructuralValidationAdapter structuralAdapter;
    private readonly ConfigPolicyRouter policyRouter;
    private readonly string auditCaller;
    private readonly string auditTarget;

    public ConfigValidationPipeline()
        : this(logsCiDirectory: null)
    {
    }

    public ConfigValidationPipeline(string? logsCiDirectory)
    {
        this.logsCiDirectory = logsCiDirectory;
        structuralAdapter = new ConfigStructuralValidationAdapter();
        policyRouter = new ConfigPolicyRouter();
        auditCaller = "ConfigValidationPipeline";
        auditTarget = "runtime-config";
    }

    public ConfigValidationResult Evaluate(string configJson, string scenarioId = "")
    {
        var structural = structuralAdapter.Validate("runtime-config", configJson, scenarioId);
        if (!structural.StructuralPassed)
        {
            var reasonIdentifiers = structural.Errors
                .Select(error => error.Code)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return BuildResult(
                terminalOutcome: "reject",
                structuralPassed: false,
                semanticChecksExecuted: false,
                semanticReasonIdentifiers: reasonIdentifiers,
                policyActions: Array.Empty<PolicyActionRecord>(),
                scenarioId: scenarioId);
        }

        var root = ConfigJson.Parse(configJson);
        var semanticReasons = EvaluateSemanticReasons(root);
        var routing = policyRouter.RouteSemanticViolations(semanticReasons);
        return BuildResult(
            terminalOutcome: routing.TerminalOutcome,
            structuralPassed: true,
            semanticChecksExecuted: true,
            semanticReasonIdentifiers: routing.ReasonIdentifiers,
            policyActions: routing.PolicyActions,
            scenarioId: scenarioId);
    }

    public ConfigValidationResult EvaluateConfig(string configJson, string scenarioId = "")
    {
        return Evaluate(configJson, scenarioId);
    }

    public ConfigValidationResult Validate(string configJson, string scenarioId = "")
    {
        return Evaluate(configJson, scenarioId);
    }

    public ConfigValidationResult ValidateConfig(string configJson, string scenarioId = "")
    {
        return Evaluate(configJson, scenarioId);
    }

    public ConfigValidationResult EvaluateAndWriteArtifacts(string configJson, string scenarioId = "")
    {
        return Evaluate(configJson, scenarioId);
    }

    public ConfigValidationResult ValidateAndWriteArtifacts(string configJson, string scenarioId = "")
    {
        return Evaluate(configJson, scenarioId);
    }

    private ConfigValidationResult BuildResult(
        string terminalOutcome,
        bool structuralPassed,
        bool semanticChecksExecuted,
        IReadOnlyList<string> semanticReasonIdentifiers,
        IReadOnlyList<PolicyActionRecord> policyActions,
        string scenarioId)
    {
        var normalizedOutcome = terminalOutcome.Trim().ToLowerInvariant();
        var reasonIdentifiers = semanticReasonIdentifiers.Count == 0 && normalizedOutcome == "accept"
            ? Array.Empty<string>()
            : semanticReasonIdentifiers.ToArray();
        var policyTrace = BuildPolicyTrace(normalizedOutcome, structuralPassed, semanticChecksExecuted, reasonIdentifiers);
        var decisionStage = !structuralPassed ? "structural" : "policy-routing";

        var outputRoot = ResolveLogsCiDirectory();
        Directory.CreateDirectory(outputRoot);
        var filePrefix = string.IsNullOrWhiteSpace(scenarioId) ? "config-validation" : scenarioId.Trim();
        filePrefix = SanitizeFileName(filePrefix);
        var auditPath = Path.Combine(outputRoot, $"{filePrefix}.audit.json");
        var summaryPath = Path.Combine(outputRoot, $"{filePrefix}.summary.json");

        var artifact = new
        {
            terminalOutcome = normalizedOutcome,
            decisionStage,
            policyTrace,
            reasonIdentifiers = reasonIdentifiers.ToArray(),
            caller = auditCaller,
            target = auditTarget
        };
        var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(auditPath, json);
        File.WriteAllText(summaryPath, json);

        return new ConfigValidationResult(
            TerminalOutcome: normalizedOutcome,
            StructuralPassed: structuralPassed,
            SemanticChecksExecuted: semanticChecksExecuted,
            SemanticViolationReasonKeys: semanticReasonIdentifiers.ToArray(),
            PolicyActions: policyActions.ToArray(),
            ReasonIdentifiers: reasonIdentifiers,
            AuditRecordPath: auditPath,
            CiSummaryPath: summaryPath,
            DecisionStage: decisionStage,
            PolicyTrace: policyTrace);
    }

    private static IReadOnlyList<string> EvaluateSemanticReasons(JsonElement root)
    {
        var reasons = new List<string>();

        var profile = GetString(root, "profile");
        if (string.IsNullOrWhiteSpace(profile))
        {
            reasons.Add("CONFIG_PROFILE_EMPTY");
        }
        else if (!string.Equals(profile, "standard", StringComparison.Ordinal))
        {
            reasons.Add("CONFIG_PROFILE_UNKNOWN");
        }

        var difficulty = GetString(root, "difficulty");
        if (string.IsNullOrWhiteSpace(difficulty))
        {
            reasons.Add("CONFIG_DIFFICULTY_UNKNOWN");
        }
        else if (!string.Equals(difficulty, "normal", StringComparison.Ordinal))
        {
            reasons.Add("CONFIG_DIFFICULTY_UNKNOWN");
        }

        var maxPlayers = GetInt(root, "maxPlayers");
        if (maxPlayers < 1)
        {
            reasons.Add("CONFIG_MAX_PLAYERS_BELOW_MINIMUM");
        }

        return reasons;
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static int GetInt(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private static IReadOnlyList<string> BuildPolicyTrace(
        string terminalOutcome,
        bool structuralPassed,
        bool semanticChecksExecuted,
        IReadOnlyList<string> reasons)
    {
        var trace = new List<string>();
        trace.Add(structuralPassed ? "structural:pass" : "structural:reject");
        trace.Add(semanticChecksExecuted ? "semantic:evaluated" : "semantic:skipped");
        trace.Add("policy:" + terminalOutcome);
        foreach (var reason in reasons)
        {
            trace.Add("reason:" + reason);
        }

        return trace;
    }

    private string ResolveLogsCiDirectory()
    {
        if (!string.IsNullOrWhiteSpace(logsCiDirectory))
        {
            var rawPath = logsCiDirectory!.Trim();
            var normalized = rawPath.Replace('\\', '/');
            if (normalized.Contains("..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("logs/ci output path is outside allowed boundary");
            }

            if (Path.IsPathRooted(rawPath))
            {
                if (!ContainsLogsCiBoundary(Path.GetFullPath(rawPath)))
                {
                    throw new InvalidOperationException("logs/ci output path is outside allowed boundary");
                }
            }
            else if (!normalized.StartsWith("logs/ci", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("logs/ci output path is outside allowed boundary");
            }

            return rawPath;
        }

        return Path.Combine("logs", "ci", DateTime.UtcNow.ToString("yyyy-MM-dd"), "config-validation");
    }

    private static bool ContainsLogsCiBoundary(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/logs/ci", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = fileName
            .Select(ch => invalidChars.Contains(ch) ? '-' : ch)
            .ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "config-validation" : sanitized;
    }
}
