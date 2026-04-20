using System.Text.Json;

namespace Game.Core.Services;

public enum ConfigTerminalOutcome
{
    Accept,
    Reject,
    Fallback
}

public enum ConfigDecisionStage
{
    Structural,
    Semantic,
    PolicyRouting
}

public sealed record RuntimeConfigState(
    string RulesetVersion,
    string DifficultyProfileId,
    double EnemySpawnRate,
    int AutosaveIntervalSeconds)
{
    public static RuntimeConfigState CreateDefault(
        string rulesetVersion,
        string difficultyProfileId,
        double enemySpawnRate,
        int autosaveIntervalSeconds)
    {
        return new RuntimeConfigState(
            rulesetVersion,
            difficultyProfileId,
            enemySpawnRate,
            autosaveIntervalSeconds);
    }
}

public sealed record RuntimeConfigPatch(
    string? RulesetVersion,
    string? DifficultyProfileId,
    double? EnemySpawnRate,
    int? AutosaveIntervalSeconds)
{
    public static RuntimeConfigPatch Empty { get; } = new(null, null, null, null);

    public RuntimeConfigPatch WithRulesetVersion(string value) => this with { RulesetVersion = value };

    public RuntimeConfigPatch WithDifficultyProfileId(string value) => this with { DifficultyProfileId = value };

    public RuntimeConfigPatch WithEnemySpawnRate(double value) => this with { EnemySpawnRate = value };

    public RuntimeConfigPatch WithAutosaveIntervalSeconds(int value) => this with { AutosaveIntervalSeconds = value };
}

public sealed record ConfigApplicationGuardResult(
    bool IsAccepted,
    RuntimeConfigState State,
    IReadOnlyList<string> RejectedFields,
    IReadOnlyList<string> AppliedFallbackFields);

public sealed record CoreLoopInput(int TurnIndex, int PlayerPosition, int DiceRoll, int Treasury)
{
    public static CoreLoopInput Create(int turnIndex, int playerPosition, int diceRoll, int treasury)
    {
        return new CoreLoopInput(turnIndex, playerPosition, diceRoll, treasury);
    }
}

public sealed record CoreLoopProjection(int Checksum)
{
    public static CoreLoopProjection Project(CoreLoopInput input, RuntimeConfigState state)
    {
        var checksum = input.TurnIndex * 97
                       + input.PlayerPosition * 53
                       + input.DiceRoll * 31
                       + input.Treasury * 7
                       + state.AutosaveIntervalSeconds * 5
                       + (int)Math.Round(state.EnemySpawnRate * 100.0)
                       + state.DifficultyProfileId.GetHashCode(StringComparison.Ordinal)
                       + state.RulesetVersion.GetHashCode(StringComparison.Ordinal);
        return new CoreLoopProjection(checksum);
    }
}

public sealed record ConfigStageTrace(ConfigDecisionStage Stage, bool ProducedTerminalOutcome);

public sealed record ConfigAuditEntry(ConfigTerminalOutcome Outcome, string ReasonId, ConfigDecisionStage Stage);

public sealed record PolicyActionRecord(string ActionName, string ReasonKey);

public sealed record NormalizedStructuralError(string Code, string Location, string Message, string SchemaId);

public sealed record ConfigStructuralValidationResult(
    bool StructuralPassed,
    bool SemanticValidationStarted,
    string AdapterId,
    IReadOnlyList<NormalizedStructuralError> Errors);

public sealed record ConfigPolicyRoutingResult(
    string TerminalOutcome,
    IReadOnlyList<PolicyActionRecord> PolicyActions,
    IReadOnlyList<string> ReasonIdentifiers);

public sealed record ConfigValidationResult(
    string TerminalOutcome,
    bool StructuralPassed,
    bool SemanticChecksExecuted,
    IReadOnlyList<string> SemanticViolationReasonKeys,
    IReadOnlyList<PolicyActionRecord> PolicyActions,
    IReadOnlyList<string> ReasonIdentifiers,
    string AuditRecordPath,
    string CiSummaryPath,
    string DecisionStage,
    IReadOnlyList<string> PolicyTrace);

public sealed record ConfigPipelineEvaluationResult(
    ConfigTerminalOutcome Outcome,
    string ReasonId,
    ConfigDecisionStage TerminalStage,
    IReadOnlyList<ConfigTerminalOutcome> TerminalOutcomes,
    IReadOnlyList<ConfigStageTrace> StageTrace,
    IReadOnlyList<ConfigAuditEntry> AuditEntries,
    CoreLoopProjection CoreProjection);

public static class ConfigJson
{
    public static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
