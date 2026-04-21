using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed class Task30CompletionScopeGuardService
{
    private const string LockedScope = "PRD:T30:PerformanceTargets";

    public ScopeDecision Evaluate(IReadOnlyCollection<ChangeEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var hasLockedScopeEvidence = evidence.Any(x => string.Equals(x.Scope, LockedScope, StringComparison.Ordinal));
        if (!hasLockedScopeEvidence)
        {
            return ScopeDecision.Rejected("missing locked scope evidence");
        }

        var hasOutOfScopeEvidence = evidence.Any(x => !string.Equals(x.Scope, LockedScope, StringComparison.Ordinal));
        if (hasOutOfScopeEvidence)
        {
            return ScopeDecision.Rejected("out-of-scope evidence detected");
        }

        return ScopeDecision.Accepted();
    }
}
