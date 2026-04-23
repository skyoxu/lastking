using System.IO;
using Game.Core.Services;
using Godot;
using Godot.Collections;

namespace Game.Godot.Adapters.Performance;

public partial class Task30PerformanceGateBridge : Node
{
    private readonly PerformanceGateVerdictService gateVerdictService = new();
    private readonly PerformanceGateArtifactContractService artifactContractService = new();
    private readonly PerformanceBaselineVarianceService baselineVarianceService = new();

    public Dictionary EvaluateWindowsBaseline(double averageFps, double onePercentLowFps)
    {
        var verdict = gateVerdictService.EvaluateWindowsBaseline(averageFps, onePercentLowFps);
        return ToVerdictPayload(verdict);
    }

    public Dictionary EvaluateFixedSeedGate(
        string platform,
        string seedMode,
        double headlessFps1PctLow,
        double headlessAverageFps,
        double playableFps1PctLow,
        double playableAverageFps)
    {
        var verdict = gateVerdictService.EvaluateFixedSeedGate(
            platform,
            seedMode,
            new PerformanceGateRunMetrics(headlessFps1PctLow, headlessAverageFps),
            new PerformanceGateRunMetrics(playableFps1PctLow, playableAverageFps));
        return ToVerdictPayload(verdict);
    }

    public Dictionary EvaluateBaselineVariance(
        bool cameraLocked,
        bool scriptedSession,
        double varianceWindowPercent,
        double runAFps1Low,
        double runAAverageFps,
        double runBFps1Low,
        double runBAverageFps)
    {
        var profile = new BaselineCaptureProfile(
            cameraLocked ? "camera/locked" : string.Empty,
            scriptedSession ? "session/scripted" : string.Empty,
            varianceWindowPercent,
            new PerformanceMetric(runAFps1Low, runAAverageFps),
            new PerformanceMetric(runBFps1Low, runBAverageFps));
        var withinVariance = baselineVarianceService.IsDeterministic(profile);
        return new Dictionary
        {
            { "eligible", cameraLocked && scriptedSession },
            { "within_variance", withinVariance }
        };
    }

    public Dictionary ValidatePerfGateArtifactPath(string relativePath)
    {
        var repoRoot = ProjectSettings.GlobalizePath("res://");
        var isAllowedPath = artifactContractService.IsAllowedArtifactPath(repoRoot, relativePath);
        if (!isAllowedPath)
        {
            return new Dictionary
            {
                { "valid", false },
                { "absolute_path", string.Empty }
            };
        }

        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(repoRoot, normalizedPath));
        var isValid = artifactContractService.IsArtifactContractValidFromFile(absolutePath);
        return new Dictionary
        {
            { "valid", isValid },
            { "absolute_path", absolutePath }
        };
    }

    private static Dictionary ToVerdictPayload(PerformanceVerdict verdict)
    {
        var passed = verdict == PerformanceVerdict.Pass;
        return new Dictionary
        {
            { "passed", passed },
            { "verdict", passed ? "PASS" : "FAIL" }
        };
    }
}
