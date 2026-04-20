using System.Text.Json;

namespace Game.Core.Services;

public sealed record GameplayTuningGovernanceRequest(
    string? ScenarioId,
    string? CandidateConfigJson,
    string? ConfigJson,
    string? CandidateJson,
    string? LogsCiDirectory,
    IReadOnlyList<string>? GovernanceCriteria,
    IReadOnlyList<string>? Criteria);

public sealed record GameplayTuningGovernanceResult(
    bool Accepted,
    string Decision,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<string> EvaluatedCriteria,
    string AuditRecordPath,
    string CiSummaryPath);

public sealed class GameplayTuningGovernanceService
{
    private readonly ConfigValidationPipeline pipeline;

    public GameplayTuningGovernanceService()
        : this(logsCiDirectory: null)
    {
    }

    public GameplayTuningGovernanceService(string? logsCiDirectory)
    {
        pipeline = new ConfigValidationPipeline(logsCiDirectory);
    }

    public GameplayTuningGovernanceResult EvaluatePromotionAttempt(
        string candidateConfigJson,
        string logsCiDirectory,
        string scenarioId,
        IReadOnlyList<string>? governanceCriteria = null)
    {
        return EvaluateInternal(
            candidateConfigJson: candidateConfigJson,
            logsCiDirectory: logsCiDirectory,
            scenarioId: scenarioId,
            governanceCriteria: governanceCriteria);
    }

    public GameplayTuningGovernanceResult EvaluatePromotion(
        string candidateConfigJson,
        string logsCiDirectory,
        string scenarioId,
        IReadOnlyList<string>? governanceCriteria = null)
    {
        return EvaluateInternal(
            candidateConfigJson: candidateConfigJson,
            logsCiDirectory: logsCiDirectory,
            scenarioId: scenarioId,
            governanceCriteria: governanceCriteria);
    }

    public GameplayTuningGovernanceResult EvaluatePromotionAttempt(GameplayTuningGovernanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var candidateConfigJson = request.CandidateConfigJson
                                  ?? request.ConfigJson
                                  ?? request.CandidateJson
                                  ?? string.Empty;
        var criteria = request.GovernanceCriteria?.Count > 0
            ? request.GovernanceCriteria
            : request.Criteria;
        return EvaluateInternal(
            candidateConfigJson: candidateConfigJson,
            logsCiDirectory: request.LogsCiDirectory ?? string.Empty,
            scenarioId: request.ScenarioId ?? string.Empty,
            governanceCriteria: criteria);
    }

    public GameplayTuningGovernanceResult EvaluatePromotionAttempt(JsonElement request)
    {
        var candidateConfigJson = ReadOptionalString(request, "candidateConfigJson")
                                  ?? ReadOptionalString(request, "configJson")
                                  ?? ReadOptionalString(request, "candidateJson")
                                  ?? string.Empty;
        var logsCiDirectory = ReadOptionalString(request, "logsCiDirectory") ?? string.Empty;
        var scenarioId = ReadOptionalString(request, "scenarioId")
                         ?? ReadOptionalString(request, "promotionId")
                         ?? ReadOptionalString(request, "attemptId")
                         ?? string.Empty;
        var criteria = ReadOptionalStringList(request, "governanceCriteria");
        if (criteria.Count == 0)
        {
            criteria = ReadOptionalStringList(request, "criteria");
        }

        return EvaluateInternal(
            candidateConfigJson: candidateConfigJson,
            logsCiDirectory: logsCiDirectory,
            scenarioId: scenarioId,
            governanceCriteria: criteria);
    }

    public GameplayTuningGovernanceResult EvaluatePromotionAttempt(JsonDocument request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return EvaluatePromotionAttempt(request.RootElement);
    }

    private GameplayTuningGovernanceResult EvaluateInternal(
        string candidateConfigJson,
        string logsCiDirectory,
        string scenarioId,
        IReadOnlyList<string>? governanceCriteria)
    {
        var criteria = (governanceCriteria ?? Array.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        if (criteria.Length == 0)
        {
            var reject = BuildMissingCriteriaReject(logsCiDirectory, scenarioId);
            return reject;
        }

        var result = pipeline.Evaluate(candidateConfigJson, scenarioId);
        var decision = string.Equals(result.TerminalOutcome, "accept", StringComparison.OrdinalIgnoreCase)
            ? "allow"
            : "reject";
        WriteEvaluatedCriteriaIntoArtifacts(
            result.AuditRecordPath,
            result.CiSummaryPath,
            criteria,
            scenarioId: string.IsNullOrWhiteSpace(scenarioId) ? "governance-eval" : scenarioId,
            decision: decision);
        return new GameplayTuningGovernanceResult(
            Accepted: string.Equals(decision, "allow", StringComparison.Ordinal),
            Decision: decision,
            ReasonCodes: result.ReasonIdentifiers,
            EvaluatedCriteria: criteria,
            AuditRecordPath: result.AuditRecordPath,
            CiSummaryPath: result.CiSummaryPath);
    }

    private GameplayTuningGovernanceResult BuildMissingCriteriaReject(string logsCiDirectory, string scenarioId)
    {
        var outputDir = ResolveOutputDirectory(logsCiDirectory);
        Directory.CreateDirectory(outputDir);
        var sanitizedScenario = SanitizeFileName(string.IsNullOrWhiteSpace(scenarioId) ? "governance-missing-criteria" : scenarioId);
        var auditPath = Path.Combine(outputDir, $"{sanitizedScenario}.audit.json");
        var summaryPath = Path.Combine(outputDir, $"{sanitizedScenario}.summary.json");
        var reason = "GOVERNANCE_CRITERIA_MISSING";
        var payload = new
        {
            decision = "reject",
            terminalOutcome = "reject",
            reasonIdentifiers = new[] { reason },
            evaluatedCriteria = Array.Empty<string>(),
            scenarioId = string.IsNullOrWhiteSpace(scenarioId) ? "governance-missing-criteria" : scenarioId
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(auditPath, json);
        File.WriteAllText(summaryPath, json);
        return new GameplayTuningGovernanceResult(
            Accepted: false,
            Decision: "reject",
            ReasonCodes: new[] { reason },
            EvaluatedCriteria: Array.Empty<string>(),
            AuditRecordPath: auditPath,
            CiSummaryPath: summaryPath);
    }

    private static void WriteEvaluatedCriteriaIntoArtifacts(
        string auditPath,
        string summaryPath,
        IReadOnlyList<string> criteria,
        string scenarioId,
        string decision)
    {
        PatchArtifact(auditPath, criteria, scenarioId, decision);
        PatchArtifact(summaryPath, criteria, scenarioId, decision);
    }

    private static void PatchArtifact(
        string artifactPath,
        IReadOnlyList<string> criteria,
        string scenarioId,
        string decision)
    {
        if (!File.Exists(artifactPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        var root = document.RootElement;
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                data[property.Name] = ConvertJsonValue(property.Value);
            }
        }

        data["decision"] = decision;
        data["evaluatedCriteria"] = criteria.ToArray();
        if (!data.ContainsKey("governanceCriteria"))
        {
            data["governanceCriteria"] = criteria.ToArray();
        }
        data["scenarioId"] = scenarioId;
        data["promotionId"] = scenarioId;

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(artifactPath, json);
    }

    private static object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out var intValue) => intValue,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonValue).ToArray(),
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertJsonValue(property.Value),
                StringComparer.OrdinalIgnoreCase),
            _ => value.GetRawText()
        };
    }

    private static string ResolveOutputDirectory(string logsCiDirectory)
    {
        if (!string.IsNullOrWhiteSpace(logsCiDirectory))
        {
            return logsCiDirectory;
        }

        return Path.Combine("logs", "ci", DateTime.UtcNow.ToString("yyyy-MM-dd"), "task-38-governance");
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "governance" : sanitized;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static IReadOnlyList<string> ReadOptionalStringList(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return Array.Empty<string>();
        }

        if (property.ValueKind == JsonValueKind.Array)
        {
            return property.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var value = property.GetString() ?? string.Empty;
            return value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToArray();
        }

        return Array.Empty<string>();
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
