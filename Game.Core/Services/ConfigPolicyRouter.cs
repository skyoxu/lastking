namespace Game.Core.Services;

public sealed class ConfigPolicyRouter
{
    public ConfigPolicyRoutingResult RouteSemanticViolations(IReadOnlyList<string> semanticReasonIdentifiers)
    {
        if (semanticReasonIdentifiers is null || semanticReasonIdentifiers.Count == 0)
        {
            return new ConfigPolicyRoutingResult(
                TerminalOutcome: "accept",
                PolicyActions: Array.Empty<PolicyActionRecord>(),
                ReasonIdentifiers: Array.Empty<string>());
        }

        var actions = semanticReasonIdentifiers
            .Select(reason => new PolicyActionRecord(NormalizeOutcome(reason), reason))
            .ToArray();

        var terminalOutcome = actions.Any(a => a.ActionName == "reject") ? "reject" : "fallback";
        return new ConfigPolicyRoutingResult(
            TerminalOutcome: terminalOutcome,
            PolicyActions: actions,
            ReasonIdentifiers: semanticReasonIdentifiers.ToArray());
    }

    private static string NormalizeOutcome(string reasonIdentifier)
    {
        if (string.Equals(reasonIdentifier, "CONFIG_PROFILE_EMPTY", StringComparison.Ordinal) ||
            string.Equals(reasonIdentifier, "CONFIG_MAX_PLAYERS_BELOW_MINIMUM", StringComparison.Ordinal))
        {
            return "fallback";
        }

        if (string.Equals(reasonIdentifier, "CONFIG_PROFILE_UNKNOWN", StringComparison.Ordinal) ||
            string.Equals(reasonIdentifier, "CONFIG_DIFFICULTY_UNKNOWN", StringComparison.Ordinal) ||
            string.Equals(reasonIdentifier, "CONFIG_REASON_NOT_MAPPED", StringComparison.Ordinal))
        {
            return "reject";
        }

        return "fallback";
    }
}
