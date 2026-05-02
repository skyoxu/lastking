using System;
using Game.Core.Contracts.Lastking;

namespace Game.Core.Services;

public sealed record HudAfterActionInputs(
    string Outcome,
    int? DayNumber,
    CastleHpChanged? CastleHp,
    WaveSpawned? Wave,
    ResourcesChanged? Resources,
    TaxCollected? Tax,
    TechApplied? Tech,
    RewardOffered? Reward);

public sealed record HudAfterActionPresentation(
    string OutcomeText,
    string PromptText,
    string ResourceText,
    string BuildText,
    string ProgressionText);

public sealed class HudAfterActionComposer
{
    public HudAfterActionPresentation Compose(HudAfterActionInputs input)
    {
        var normalizedOutcome = (input.Outcome ?? string.Empty).Trim();
        var dayText = input.DayNumber.HasValue ? $" day={input.DayNumber.Value}" : string.Empty;
        var hp = input.CastleHp?.CurrentHp;
        var spawn = input.Wave?.SpawnCount;

        var isTerminal = IsTerminalOutcome(normalizedOutcome);
        var isNonTerminal = !isTerminal;
        var outcomeText = isNonTerminal
            ? "Outcome: n/a"
            : BuildOutcomeText(normalizedOutcome, dayText, hp, spawn);

        var promptText = BuildPromptText(normalizedOutcome, hp, spawn, input.Resources?.Gold);
        var resourceText = BuildResourceText(input.Resources);
        var buildText = BuildBuildText(input.Tax);
        var progressionText = BuildProgressionText(input.Tech, input.Reward);

        return new HudAfterActionPresentation(
            OutcomeText: outcomeText,
            PromptText: promptText,
            ResourceText: resourceText,
            BuildText: buildText,
            ProgressionText: progressionText);
    }

    private static bool IsTerminalOutcome(string outcome)
    {
        return string.Equals(outcome, "win", StringComparison.OrdinalIgnoreCase)
            || string.Equals(outcome, "loss", StringComparison.OrdinalIgnoreCase)
            || string.Equals(outcome, "lose", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildOutcomeText(string outcome, string dayText, int? hp, int? spawn)
    {
        if (!hp.HasValue && !spawn.HasValue)
        {
            return $"Outcome: {outcome}{dayText}";
        }

        var pressure = hp.HasValue
            ? hp.Value <= 20
                ? "critical"
                : hp.Value <= 60
                    ? "high"
                    : "stable"
            : "n/a";
        var hpText = hp.HasValue ? hp.Value.ToString() : "n/a";
        var spawnText = spawn.HasValue ? spawn.Value.ToString() : "n/a";
        return $"Outcome: {outcome}{dayText} pressure={pressure} (hp={hpText}) spawned={spawnText}";
    }

    private static string BuildPromptText(string outcome, int? hp, int? spawn, int? gold)
    {
        if (!IsTerminalOutcome(outcome))
        {
            return "Prompt: n/a";
        }

        if (hp.HasValue && hp.Value <= 60 || spawn.HasValue && spawn.Value >= 6)
        {
            return "Prompt: training: reinforce frontline";
        }

        if (gold.HasValue && gold.Value < 130)
        {
            return "Prompt: economy: increase income";
        }

        return "Prompt: build: expand defenses";
    }

    private static string BuildResourceText(ResourcesChanged? resources)
    {
        if (resources is null)
        {
            return "Resources: gold=n/a iron=n/a pop=n/a";
        }

        return $"Resources: gold={resources.Gold} iron={resources.Iron} pop={resources.PopulationCap}";
    }

    private static string BuildBuildText(TaxCollected? tax)
    {
        if (tax is null)
        {
            return "Build: tax=n/a total_gold=n/a";
        }

        return $"Build: tax={tax.GoldDelta} total_gold={tax.TotalGold} residence={tax.ResidenceId}";
    }

    private static string BuildProgressionText(TechApplied? tech, RewardOffered? reward)
    {
        var techText = tech is null
            ? "tech=n/a:n/a n/a->n/a"
            : $"tech={tech.TechId}:{tech.StatKey} {tech.PreviousValue}->{tech.CurrentValue}";
        var rewardText = reward is null
            ? "reward=n/a"
            : $"reward={reward.OptionA}, {reward.OptionB}, {reward.OptionC}";
        return $"Progression: {techText} {rewardText}";
    }
}
