namespace Game.Core.Services;

public static class ConfigApplicationGuards
{
    public static ConfigApplicationGuardResult Apply(RuntimeConfigState initialState, RuntimeConfigPatch candidate)
    {
        var rejectedFields = new List<string>();
        var fallbackFields = new List<string>();

        if (candidate.RulesetVersion is not null && !string.Equals(candidate.RulesetVersion, initialState.RulesetVersion, StringComparison.Ordinal))
        {
            rejectedFields.Add("rulesetVersion");
        }

        if (candidate.DifficultyProfileId is not null && !string.Equals(candidate.DifficultyProfileId, initialState.DifficultyProfileId, StringComparison.Ordinal))
        {
            rejectedFields.Add("difficultyProfileId");
        }

        if (rejectedFields.Count > 0)
        {
            return new ConfigApplicationGuardResult(
                IsAccepted: false,
                State: initialState,
                RejectedFields: rejectedFields,
                AppliedFallbackFields: Array.Empty<string>());
        }

        var nextEnemySpawnRate = initialState.EnemySpawnRate;
        if (candidate.EnemySpawnRate is not null)
        {
            if (candidate.EnemySpawnRate.Value <= 0.0)
            {
                fallbackFields.Add("enemySpawnRate");
            }
            else
            {
                nextEnemySpawnRate = candidate.EnemySpawnRate.Value;
            }
        }

        var nextAutosaveIntervalSeconds = initialState.AutosaveIntervalSeconds;
        if (candidate.AutosaveIntervalSeconds is not null)
        {
            if (candidate.AutosaveIntervalSeconds.Value <= 0)
            {
                fallbackFields.Add("autosaveIntervalSeconds");
            }
            else
            {
                nextAutosaveIntervalSeconds = candidate.AutosaveIntervalSeconds.Value;
            }
        }

        var nextState = initialState with
        {
            EnemySpawnRate = nextEnemySpawnRate,
            AutosaveIntervalSeconds = nextAutosaveIntervalSeconds
        };

        return new ConfigApplicationGuardResult(
            IsAccepted: true,
            State: nextState,
            RejectedFields: Array.Empty<string>(),
            AppliedFallbackFields: fallbackFields);
    }
}
