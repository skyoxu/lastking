using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed class PerformancePhaseEvidenceService
{
    private static readonly HashSet<PerformanceEvidencePhase> RequiredPhases = new()
    {
        PerformanceEvidencePhase.BaselineCapture,
        PerformanceEvidencePhase.HotspotIsolation,
        PerformanceEvidencePhase.FrameBudgetRemediation,
        PerformanceEvidencePhase.StressValidation,
        PerformanceEvidencePhase.GateArtifactOutput
    };

    public bool IsBlocked(ISet<PerformanceEvidencePhase> collectedPhases)
    {
        ArgumentNullException.ThrowIfNull(collectedPhases);
        return RequiredPhases.Any(requiredPhase => !collectedPhases.Contains(requiredPhase));
    }
}
