using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigValidationPipelineTests
{
    [Fact]
    public void ShouldKeepCoreLoopDecisionStable_WhenValidationFlowRunsForIdenticalInputs()
    {
        using var workspace = TempWorkspace.Create();
        var pipeline = new ConfigValidationPipeline(workspace.LogsCiDirectory);
        const string validJson = """
                                 {
                                   "profile": "standard",
                                   "difficulty": "normal",
                                   "maxPlayers": 4
                                 }
                                 """;
        var baselineState = RuntimeConfigState.CreateDefault(
            rulesetVersion: "1.0.0",
            difficultyProfileId: "standard",
            enemySpawnRate: 1.0,
            autosaveIntervalSeconds: 120);
        var fallbackPatch = RuntimeConfigPatch.Empty
            .WithEnemySpawnRate(-2.0)
            .WithAutosaveIntervalSeconds(0);
        var loopInput = CoreLoopInput.Create(turnIndex: 8, playerPosition: 3, diceRoll: 6, treasury: 240);

        var firstEvaluation = pipeline.Evaluate(validJson, "core-loop-stable-first");
        var secondEvaluation = pipeline.Evaluate(validJson, "core-loop-stable-second");
        var firstGuardResult = ConfigApplicationGuards.Apply(baselineState, fallbackPatch);
        var secondGuardResult = ConfigApplicationGuards.Apply(baselineState, fallbackPatch);
        var firstProjection = CoreLoopProjection.Project(loopInput, firstGuardResult.State);
        var secondProjection = CoreLoopProjection.Project(loopInput, secondGuardResult.State);

        firstEvaluation.TerminalOutcome.Should().Be("accept");
        secondEvaluation.TerminalOutcome.Should().Be("accept");
        firstProjection.Should().Be(secondProjection);
    }

    // ACC:T37.1
    // acceptance: ACC:T37.11
    [Theory]
    [MemberData(nameof(ConfigMatrixCases))]
    public void ShouldProducePolicyConsistentOutcomeAndAuditLog_WhenEvaluatingConfigMatrix(
        string scenarioId,
        string configJson,
        string expectedOutcome,
        string[] expectedReasonIdentifiers)
    {
        using var workspace = TempWorkspace.Create();
        var pipeline = new ConfigValidationPipeline(workspace.LogsCiDirectory);

        var result = pipeline.Evaluate(configJson, scenarioId);
        using var audit = ReadJsonFile(result.AuditRecordPath);

        result.TerminalOutcome.Should().Be(expectedOutcome);
        result.ReasonIdentifiers.Should().BeEquivalentTo(expectedReasonIdentifiers);
        GetRequiredString(audit.RootElement, "terminalOutcome").Should().Be(expectedOutcome);
        GetRequiredStringArray(audit.RootElement, "reasonIdentifiers").Should().BeEquivalentTo(expectedReasonIdentifiers);
    }

    // acceptance: ACC:T37.12
    [Theory]
    [MemberData(nameof(ConfigMatrixCases))]
    public void ShouldEmitExactlyOneTerminalOutcome_WhenEvaluatingOneConfigInput(
        string scenarioId,
        string configJson,
        string expectedOutcome,
        string[] expectedReasonIdentifiers)
    {
        using var workspace = TempWorkspace.Create();
        var pipeline = new ConfigValidationPipeline(workspace.LogsCiDirectory);

        var result = pipeline.Evaluate(configJson, scenarioId);
        using var summary = ReadJsonFile(result.CiSummaryPath);
        var policyTrace = GetRequiredStringArray(summary.RootElement, "policyTrace");
        var policyMarkers = policyTrace.Where(item => item.StartsWith("policy:", StringComparison.Ordinal)).ToArray();

        result.TerminalOutcome.Should().Be(expectedOutcome);
        result.ReasonIdentifiers.Should().BeEquivalentTo(expectedReasonIdentifiers);
        policyMarkers.Should().ContainSingle().Which.Should().Be("policy:" + expectedOutcome);
    }

    // acceptance: ACC:T37.13
    // acceptance: ACC:T37.3
    [Theory]
    [MemberData(nameof(StageRoutingCases))]
    public void ShouldExecuteLayeredValidationOrderAndTraceTerminalStage_WhenConfigStopsAtSpecificStage(
        string scenarioId,
        string configJson,
        string expectedDecisionStage,
        string expectedTerminalOutcome,
        string[] expectedReasonIdentifiers,
        string structuralStageMarker,
        string semanticStageMarker,
        string policyStageMarker)
    {
        using var workspace = TempWorkspace.Create();
        var pipeline = new ConfigValidationPipeline(workspace.LogsCiDirectory);

        var result = pipeline.Evaluate(configJson, scenarioId);
        using var audit = ReadJsonFile(result.AuditRecordPath);
        var policyTrace = GetRequiredStringArray(audit.RootElement, "policyTrace");

        policyTrace.Take(3).Should().Equal(structuralStageMarker, semanticStageMarker, policyStageMarker);
        result.DecisionStage.Should().Be(expectedDecisionStage);
        result.TerminalOutcome.Should().Be(expectedTerminalOutcome);
        result.ReasonIdentifiers.Should().BeEquivalentTo(expectedReasonIdentifiers);
    }

    // acceptance: ACC:T37.8
    [Fact]
    public void ShouldReturnDeterministicOutcomeAndReason_WhenBoundaryConfigIsEvaluatedRepeatedly()
    {
        using var workspace = TempWorkspace.Create();
        var pipeline = new ConfigValidationPipeline(workspace.LogsCiDirectory);
        const string boundaryJson = """
                                    {
                                      "profile": "",
                                      "difficulty": "normal",
                                      "maxPlayers": 4
                                    }
                                    """;

        var results = Enumerable.Range(0, 5)
            .Select(index => pipeline.Evaluate(boundaryJson, "boundary-repeat-" + index))
            .ToArray();

        results.Select(result => result.TerminalOutcome).Distinct(StringComparer.Ordinal).Should().ContainSingle().Which.Should().Be("fallback");
        results.SelectMany(result => result.ReasonIdentifiers).Distinct(StringComparer.Ordinal).Should().ContainSingle().Which.Should().Be("CONFIG_PROFILE_EMPTY");
        results.Select(result => result.DecisionStage).Distinct(StringComparer.Ordinal).Should().ContainSingle().Which.Should().Be("policy-routing");
        results.Select(result => string.Join("|", result.PolicyTrace)).Distinct(StringComparer.Ordinal).Should().ContainSingle();
    }

    public static IEnumerable<object[]> ConfigMatrixCases()
    {
        yield return new object[]
        {
            "matrix-valid-accept",
            """
            {
              "profile": "standard",
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "accept",
            Array.Empty<string>()
        };
        yield return new object[]
        {
            "matrix-structural-reject",
            """
            {
              "profile": 42,
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "reject",
            new[] { "CONFIG_STRUCTURE_TYPE_MISMATCH" }
        };
        yield return new object[]
        {
            "matrix-structural-json-invalid",
            "{ invalid json",
            "reject",
            new[] { "CONFIG_STRUCTURE_JSON_INVALID" }
        };
        yield return new object[]
        {
            "matrix-boundary-fallback",
            """
            {
              "profile": "",
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "fallback",
            new[] { "CONFIG_PROFILE_EMPTY" }
        };
        yield return new object[]
        {
            "matrix-policy-reject",
            """
            {
              "profile": "unknown-profile",
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "reject",
            new[] { "CONFIG_PROFILE_UNKNOWN" }
        };
    }

    public static IEnumerable<object[]> StageRoutingCases()
    {
        yield return new object[]
        {
            "stage-structural-reject",
            """
            {
              "profile": 42,
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "structural",
            "reject",
            new[] { "CONFIG_STRUCTURE_TYPE_MISMATCH" },
            "structural:reject",
            "semantic:skipped",
            "policy:reject"
        };
        yield return new object[]
        {
            "stage-structural-json-invalid",
            "{ broken json",
            "structural",
            "reject",
            new[] { "CONFIG_STRUCTURE_JSON_INVALID" },
            "structural:reject",
            "semantic:skipped",
            "policy:reject"
        };
        yield return new object[]
        {
            "stage-policy-reject",
            """
            {
              "profile": "unknown-profile",
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "policy-routing",
            "reject",
            new[] { "CONFIG_PROFILE_UNKNOWN" },
            "structural:pass",
            "semantic:evaluated",
            "policy:reject"
        };
        yield return new object[]
        {
            "stage-policy-fallback",
            """
            {
              "profile": "",
              "difficulty": "normal",
              "maxPlayers": 4
            }
            """,
            "policy-routing",
            "fallback",
            new[] { "CONFIG_PROFILE_EMPTY" },
            "structural:pass",
            "semantic:evaluated",
            "policy:fallback"
        };
    }

    private static JsonDocument ReadJsonFile(string path)
    {
        File.Exists(path).Should().BeTrue();
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        root.TryGetProperty(propertyName, out var property).Should().BeTrue();
        property.ValueKind.Should().Be(JsonValueKind.String);
        return property.GetString() ?? string.Empty;
    }

    private static IReadOnlyList<string> GetRequiredStringArray(JsonElement root, string propertyName)
    {
        root.TryGetProperty(propertyName, out var property).Should().BeTrue();
        property.ValueKind.Should().Be(JsonValueKind.Array);
        return property.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            LogsCiDirectory = Path.Combine(rootDirectory, "logs", "ci");
            Directory.CreateDirectory(LogsCiDirectory);
        }

        public string RootDirectory { get; }

        public string LogsCiDirectory { get; }

        public static TempWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "lastking-task37-pipeline-" + Guid.NewGuid().ToString("N"));
            return new TempWorkspace(rootDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
