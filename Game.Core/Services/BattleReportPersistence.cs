using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Game.Core.Services;

public sealed class BattleReportPersistence
{
    public string Persist(BattleReportPayload payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        var stored = new StoredReportDto
        {
            MatchResult = new StoredMatchResultDto
            {
                Outcome = payload.MatchResult.Outcome,
                WavesSurvived = payload.MatchResult.WavesSurvived,
                EnemiesDefeated = payload.MatchResult.EnemiesDefeated,
                Score = payload.MatchResult.Score,
            },
            Metadata = payload.Metadata
                .Where(pair => pair.Key == "config_hash" || pair.Key == "config_version")
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };

        return JsonSerializer.Serialize(stored);
    }

    public BattleReportPayload Load(string storedPayload)
    {
        var stored = JsonSerializer.Deserialize<StoredReportDto>(storedPayload) ?? new StoredReportDto();
        var restoredMatchResult = new BattleMatchResult(
            stored.MatchResult?.Outcome ?? string.Empty,
            stored.MatchResult?.WavesSurvived ?? 0,
            stored.MatchResult?.EnemiesDefeated ?? 0,
            stored.MatchResult?.Score ?? 0);

        var restoredMetadata = new Dictionary<string, string>(
            stored.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        return new BattleReportPayload(restoredMatchResult, restoredMetadata);
    }

    private sealed class StoredReportDto
    {
        public StoredMatchResultDto? MatchResult { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }
    }

    private sealed class StoredMatchResultDto
    {
        public string Outcome { get; set; } = string.Empty;

        public int WavesSurvived { get; set; }

        public int EnemiesDefeated { get; set; }

        public int Score { get; set; }
    }
}
