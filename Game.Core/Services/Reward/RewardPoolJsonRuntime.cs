using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Core.Contracts.Lastking;

namespace Game.Core.Services.Reward;

public sealed class RewardPoolJsonRuntime
{
    private readonly Func<DateTimeOffset> utcNow;
    private Dictionary<NightType, Queue<string>> pools = new();
    private int fallbackGold;

    public RewardPoolJsonRuntime(string json, Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(utcNow);

        this.utcNow = utcNow;
        ReloadJson(json);
    }

    public void ReloadJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);
        var rewardNode = document.RootElement.GetProperty("reward");

        fallbackGold = ParseFallbackGold(rewardNode);
        pools = new Dictionary<NightType, Queue<string>>
        {
            [NightType.Normal] = new Queue<string>(ReadPool(rewardNode, "normal")),
            [NightType.Elite] = new Queue<string>(ReadPool(rewardNode, "elite")),
            [NightType.Boss] = new Queue<string>(ReadPool(rewardNode, "boss")),
        };
    }

    public NightlyRewardResult TriggerNight(
        string runId,
        int dayNumber,
        bool isEliteNight,
        bool isBossNight)
    {
        var activeNightType = ResolveNightType(isEliteNight, isBossNight);

        if (!pools.TryGetValue(activeNightType, out var activePool) || activePool.Count < 3)
        {
            return new NightlyRewardResult(
                Triggered: true,
                ActiveNightType: activeNightType,
                Choices: Array.Empty<string>(),
                OfferedEvent: null,
                GrantedFallbackGold: fallbackGold);
        }

        var optionA = activePool.Dequeue();
        var optionB = activePool.Dequeue();
        var optionC = activePool.Dequeue();
        var offeredEvent = new RewardOffered(
            runId,
            dayNumber,
            activeNightType == NightType.Elite,
            activeNightType == NightType.Boss,
            optionA,
            optionB,
            optionC,
            utcNow());

        return new NightlyRewardResult(
            Triggered: true,
            ActiveNightType: activeNightType,
            Choices: new[] { optionA, optionB, optionC },
            OfferedEvent: offeredEvent,
            GrantedFallbackGold: 0);
    }

    private static NightType ResolveNightType(bool isEliteNight, bool isBossNight)
    {
        if (isBossNight)
        {
            return NightType.Boss;
        }

        if (isEliteNight)
        {
            return NightType.Elite;
        }

        return NightType.Normal;
    }

    private static int ParseFallbackGold(JsonElement rewardNode)
    {
        if (rewardNode.TryGetProperty("fallback_gold", out var fallbackNode)
            && fallbackNode.ValueKind == JsonValueKind.Number
            && fallbackNode.TryGetInt32(out var value)
            && value >= 0)
        {
            return value;
        }

        return 0;
    }

    private static IReadOnlyList<string> ReadPool(JsonElement rewardNode, string poolKey)
    {
        if (!rewardNode.TryGetProperty("pools", out var poolsNode))
        {
            return Array.Empty<string>();
        }

        if (!poolsNode.TryGetProperty(poolKey, out var poolNode) || poolNode.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var values = new List<string>();
        foreach (var item in poolNode.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }
}
