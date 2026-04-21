using System.Collections.Generic;

namespace Game.Core.Services;

public sealed record PerformanceMetric(double Fps1Low, double AverageFps);
public sealed record PerformanceGateRunMetrics(double Fps1Low, double AverageFps);

public sealed record PerformanceEvidence(
    string SceneSet,
    int FixedSeed,
    string RunMode,
    PerformanceMetric? Metrics);

public sealed record PerformancePairingResult(bool IsPairable, double? DeltaFps1Low, double? DeltaAverageFps);

public sealed record PerformanceEvaluationInput(
    double BaselineAverageFps,
    double CurrentAverageFps,
    double BaselineOnePercentLowFps,
    double CurrentOnePercentLowFps,
    double DeclaredVarianceWindow,
    bool HasSummaryJson,
    bool HasTelemetryCsv,
    bool HasBaselineField,
    bool HasCurrentField);

public enum PerformanceVerdict
{
    Pass,
    Fail
}

public sealed record FrameTimingSample(
    string Subsystem,
    string Operation,
    double FrameTimeMs,
    string RunId,
    int FrameIndex);

public sealed record FrameTimeHotspotRow(
    string Subsystem,
    string Offender,
    double WorstFrameTimeMs,
    double AverageFrameTimeMs,
    string SampleRunId,
    int sampleCount = 1);

public sealed record FrameTimeHotspotReport(IReadOnlyList<FrameTimeHotspotRow> Rows);

public sealed record FrameMetrics(double AverageFrameMs, double OnePercentLowFrameMs)
{
    public double AverageFps => 1000d / AverageFrameMs;

    public double OnePercentLowFps => 1000d / OnePercentLowFrameMs;
}

public sealed record GameplaySnapshot(IReadOnlyList<string> Events);

public sealed record OptimizationCandidate(
    bool UpdateLoopBatchingEnabled,
    bool ObjectPoolingEnabled,
    bool ExpensiveQueryCachingEnabled,
    bool ReordersGameplayEvents);

public sealed record PerformanceRemediationResult(
    bool MeasurableImprovement,
    bool SemanticsUnchanged,
    FrameMetrics Baseline,
    FrameMetrics Optimized);

public sealed record BaselineCaptureProfile(
    string CameraPathId,
    string SessionScriptId,
    double VarianceWindowPercent,
    PerformanceMetric RunA,
    PerformanceMetric RunB);

public sealed record PerformanceRunIdentity(
    string SceneSet,
    int FixedSeed,
    string CameraPathScript,
    string LaunchPreset,
    string RunMode);

public enum ComparisonVerdict
{
    Pass,
    Fail
}

public enum PerformanceEvidencePhase
{
    BaselineCapture,
    HotspotIsolation,
    FrameBudgetRemediation,
    StressValidation,
    GateArtifactOutput
}

public sealed record ChangeEvidence(string Scope, string Artifact);

public sealed record ScopeDecision(bool IsAccepted, string Reason)
{
    public static ScopeDecision Accepted() => new(true, "accepted");

    public static ScopeDecision Rejected(string reason) => new(false, reason);
}

public sealed record PerformanceSummarySchemaEvaluation(
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> NonIntegerFields)
{
    public bool IsValid => MissingFields.Count == 0 && NonIntegerFields.Count == 0;
}
