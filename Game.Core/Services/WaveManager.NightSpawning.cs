using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed partial class WaveManager
{
    public IReadOnlyList<CadenceSpawnEmission> GenerateRegularSpawns(
        double nightDurationSeconds,
        bool isBossNight,
        IReadOnlyList<string> spawnPoints,
        double cadenceSeconds = 10.0)
    {
        var emissions = new List<CadenceSpawnEmission>();
        if (nightDurationSeconds <= 0.0 || isBossNight || spawnPoints.Count == 0 || cadenceSeconds <= 0.0)
        {
            return emissions;
        }

        var cadenceWindowEndSeconds = nightDurationSeconds * 0.8;
        for (var elapsedSeconds = 0.0; elapsedSeconds < cadenceWindowEndSeconds; elapsedSeconds += cadenceSeconds)
        {
            var spawnPointIndex = emissions.Count % spawnPoints.Count;
            emissions.Add(new CadenceSpawnEmission(elapsedSeconds, spawnPoints[spawnPointIndex]));
        }

        return emissions;
    }

    public IReadOnlyList<CadenceSpawnEmission> GenerateRegularSpawnsFromConfig(
        ConfigManager configManager,
        bool isBossNight,
        IReadOnlyList<string> spawnPoints)
    {
        ArgumentNullException.ThrowIfNull(configManager);
        var snapshot = configManager.Snapshot;
        return GenerateRegularSpawns(
            nightDurationSeconds: snapshot.NightSeconds,
            isBossNight: isBossNight,
            spawnPoints: spawnPoints,
            cadenceSeconds: snapshot.SpawnCadenceSeconds);
    }

    public IReadOnlyList<NightSpawnEmission> GenerateNightSpawns(
        NightSpawnPolicy spawnPolicy,
        bool isBossNight,
        IReadOnlyList<string> spawnPoints)
    {
        var emissions = new List<NightSpawnEmission>();
        if (spawnPolicy.NightDurationSeconds <= 0.0 ||
            spawnPolicy.CadenceSeconds <= 0.0 ||
            spawnPolicy.BossSpawnCount < 0 ||
            spawnPoints.Count == 0)
        {
            return emissions;
        }

        var openWindowEndSeconds = spawnPolicy.NightDurationSeconds * 0.8;
        if (isBossNight)
        {
            for (var bossIndex = 0; bossIndex < spawnPolicy.BossSpawnCount; bossIndex++)
            {
                var elapsedSeconds = bossIndex * spawnPolicy.CadenceSeconds;
                if (elapsedSeconds >= openWindowEndSeconds)
                {
                    break;
                }

                var spawnPointId = spawnPoints[bossIndex % spawnPoints.Count];
                emissions.Add(new NightSpawnEmission(elapsedSeconds, spawnPointId, NightEnemyType.Boss, "Boss"));
            }

            return emissions;
        }

        for (var elapsedSeconds = 0.0; elapsedSeconds < openWindowEndSeconds; elapsedSeconds += spawnPolicy.CadenceSeconds)
        {
            var spawnPointIndex = emissions.Count % spawnPoints.Count;
            emissions.Add(new NightSpawnEmission(elapsedSeconds, spawnPoints[spawnPointIndex], NightEnemyType.Regular, "Regular"));
        }

        return emissions;
    }

    public IReadOnlyList<NightSpawnEmission> GenerateNightSpawnsFromConfig(
        ConfigManager configManager,
        bool isBossNight,
        IReadOnlyList<string> spawnPoints)
    {
        ArgumentNullException.ThrowIfNull(configManager);
        var snapshot = configManager.Snapshot;
        var spawnPolicy = new NightSpawnPolicy(
            NightDurationSeconds: snapshot.NightSeconds,
            CadenceSeconds: snapshot.SpawnCadenceSeconds,
            BossSpawnCount: snapshot.BossCount);

        return GenerateNightSpawns(spawnPolicy, isBossNight, spawnPoints);
    }

    public IReadOnlyList<StateGatedSpawnEmission> GenerateCadenceDrivenSpawns(
        NightGameplayState gameplayState,
        double elapsedSeconds,
        string spawnPointId,
        string enemyId,
        double cadenceSeconds = 10.0)
    {
        var emissions = new List<StateGatedSpawnEmission>();
        if (cadenceSeconds <= 0.0 || elapsedSeconds < 0.0)
        {
            return emissions;
        }

        var isCadenceTick = Math.Abs(elapsedSeconds % cadenceSeconds) < 0.0001;
        if (!isCadenceTick || gameplayState != NightGameplayState.Night)
        {
            return emissions;
        }

        emissions.Add(new StateGatedSpawnEmission(elapsedSeconds, spawnPointId, enemyId, gameplayState));
        return emissions;
    }

    public NightSpawnFlowResult TryStartNightSpawn(NightSpawnCompositionContract composition)
    {
        var errors = new List<string>();

        if (composition.CadenceSeconds is null || composition.CadenceSeconds <= 0)
        {
            errors.Add("cadence_seconds_required");
        }

        if (composition.CadenceWindowSeconds is null || composition.CadenceWindowSeconds <= 0)
        {
            errors.Add("cadence_window_seconds_required");
        }

        if (string.IsNullOrWhiteSpace(composition.DeterministicRngKey))
        {
            errors.Add("deterministic_rng_key_required");
        }

        if (string.IsNullOrWhiteSpace(composition.DeterministicTieBreakerKey))
        {
            errors.Add("deterministic_tie_breaker_key_required");
        }

        if (composition.DeterministicSequencingKeys is null ||
            composition.DeterministicSequencingKeys.Count == 0 ||
            composition.DeterministicSequencingKeys.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("deterministic_sequencing_keys_required");
        }

        if (errors.Count > 0)
        {
            return new NightSpawnFlowResult(
                ValidationPassed: false,
                Started: false,
                Errors: errors,
                SpawnEvents: new[] { "validate" });
        }

        var spawnEvents = new List<string> { "validate", "start" };
        foreach (var sequencingKey in composition.DeterministicSequencingKeys!)
        {
            spawnEvents.Add($"emit:{sequencingKey}");
        }

        return new NightSpawnFlowResult(
            ValidationPassed: true,
            Started: true,
            Errors: Array.Empty<string>(),
            SpawnEvents: spawnEvents);
    }

    public IReadOnlyList<DeterministicNightSpawnEmission> GenerateDeterministicNightSpawns(
        NightRunConfiguration runConfiguration,
        IReadOnlyList<string> spawnPoints,
        IReadOnlyList<string> compositionPayload,
        int deterministicSeed)
    {
        var emissions = new List<DeterministicNightSpawnEmission>();
        if (runConfiguration.CadenceSeconds <= 0.0 ||
            runConfiguration.NightDurationSeconds <= 0.0 ||
            spawnPoints.Count == 0 ||
            compositionPayload.Count == 0)
        {
            return emissions;
        }

        var elapsedSeconds = 0.0;
        var rng = new Random(deterministicSeed);
        while (elapsedSeconds < runConfiguration.NightDurationSeconds)
        {
            var spawnPointId = spawnPoints[rng.Next(spawnPoints.Count)];
            var enemyId = compositionPayload[rng.Next(compositionPayload.Count)];
            emissions.Add(new DeterministicNightSpawnEmission(elapsedSeconds, spawnPointId, enemyId));
            elapsedSeconds += runConfiguration.CadenceSeconds;
        }

        return emissions;
    }

    public NightSpawnResult ProcessNightSpawn(NightSpawnComposition composition)
    {
        if (string.IsNullOrWhiteSpace(composition.EnemyArchetype))
        {
            return new NightSpawnResult(
                Emissions: Array.Empty<NightSpawnCompositionEmission>(),
                Diagnostic: new NightSpawnDiagnostic(
                    Code: "MalformedComposition",
                    Reason: "Malformed spawn composition rejected",
                    OffendingField: nameof(NightSpawnComposition.EnemyArchetype)));
        }

        if (composition.Count <= 0)
        {
            return new NightSpawnResult(
                Emissions: Array.Empty<NightSpawnCompositionEmission>(),
                Diagnostic: new NightSpawnDiagnostic(
                    Code: "MalformedComposition",
                    Reason: "Malformed spawn composition rejected",
                    OffendingField: nameof(NightSpawnComposition.Count)));
        }

        return new NightSpawnResult(
            Emissions: new[] { new NightSpawnCompositionEmission(composition.EnemyArchetype, composition.Count) },
            Diagnostic: null);
    }

    public NightChannelBoundaryResult OrchestrateNightChannels(NightChannelBoundaryRequest request)
    {
        var regularEmissionTimes = new List<int>();
        for (var second = request.RegularWindowStartSeconds;
             second < request.RegularWindowEndSeconds && second < request.NightDurationSeconds;
             second += request.RegularCadenceSeconds)
        {
            regularEmissionTimes.Add(second);
        }

        var bossEmissionCount = Math.Min(request.BossSpawnAttempts, request.BossNightLimit);
        var rejectedBossAttempts = request.BossSpawnAttempts - bossEmissionCount;

        return new NightChannelBoundaryResult(
            RegularEmissionTimes: regularEmissionTimes,
            BossEmissionCount: bossEmissionCount,
            RejectedBossAttempts: rejectedBossAttempts);
    }

    public NightTraceRunResult TryRunNightTrace(
        int seed,
        IReadOnlyList<NightTraceEntry> entries,
        int remainingBudget)
    {
        if (entries.Count == 0 ||
            entries.Any(entry =>
                string.IsNullOrWhiteSpace(entry.EnemyId) ||
                entry.Count <= 0 ||
                entry.Cadence <= 0))
        {
            return new NightTraceRunResult(
                Accepted: false,
                Trace: Array.Empty<string>(),
                RemainingBudget: remainingBudget);
        }

        var random = new Random(seed);
        var trace = new List<string>();
        var spawnCountsByEnemy = new Dictionary<string, int>(StringComparer.Ordinal);

        var maxTick = entries.Max(entry => entry.Count * entry.Cadence);
        for (var tick = 1; tick <= maxTick; tick++)
        {
            foreach (var entry in entries)
            {
                if (tick % entry.Cadence != 0)
                {
                    continue;
                }

                if (!spawnCountsByEnemy.TryGetValue(entry.EnemyId, out var spawned))
                {
                    spawned = 0;
                }

                if (spawned >= entry.Count)
                {
                    continue;
                }

                var roll = random.Next(1, 100);
                trace.Add($"spawn:{entry.EnemyId}:tick={tick}:roll={roll}");
                spawnCountsByEnemy[entry.EnemyId] = spawned + 1;
            }
        }

        var remaining = Math.Max(0, remainingBudget - trace.Count);
        return new NightTraceRunResult(
            Accepted: true,
            Trace: trace,
            RemainingBudget: remaining);
    }
}

public sealed record CadenceSpawnEmission(double ElapsedSeconds, string SpawnPointId);

public sealed record NightSpawnPolicy(double NightDurationSeconds, double CadenceSeconds, int BossSpawnCount);

public enum NightEnemyType
{
    Regular,
    Elite,
    Boss
}

public sealed record NightSpawnEmission(double ElapsedSeconds, string SpawnPointId, NightEnemyType EnemyType, string EnemyArchetype);

public enum NightGameplayState
{
    Day,
    Dusk,
    Night,
    Dawn
}

public sealed record StateGatedSpawnEmission(
    double ElapsedSeconds,
    string SpawnPointId,
    string EnemyId,
    NightGameplayState SourceState);

public sealed record NightSpawnCompositionContract(
    int? CadenceSeconds,
    int? CadenceWindowSeconds,
    string? DeterministicRngKey,
    string? DeterministicTieBreakerKey,
    IReadOnlyList<string>? DeterministicSequencingKeys);

public sealed record NightSpawnFlowResult(
    bool ValidationPassed,
    bool Started,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> SpawnEvents);

public sealed record NightRunConfiguration(double CadenceSeconds, double NightDurationSeconds);

public sealed record DeterministicNightSpawnEmission(double ElapsedSeconds, string SpawnPointId, string EnemyId);

public sealed record NightSpawnComposition(string EnemyArchetype, int Count);

public sealed record NightSpawnCompositionEmission(string EnemyArchetype, int Count);

public sealed record NightSpawnDiagnostic(string Code, string Reason, string OffendingField);

public sealed record NightSpawnResult(
    IReadOnlyList<NightSpawnCompositionEmission> Emissions,
    NightSpawnDiagnostic? Diagnostic);

public sealed record NightChannelBoundaryRequest(
    int NightDurationSeconds,
    int RegularCadenceSeconds,
    int RegularWindowStartSeconds,
    int RegularWindowEndSeconds,
    int BossNightLimit,
    int BossSpawnAttempts);

public sealed record NightChannelBoundaryResult(
    IReadOnlyList<int> RegularEmissionTimes,
    int BossEmissionCount,
    int RejectedBossAttempts);

public sealed record NightTraceEntry(string EnemyId, int Count, int Cadence);

public sealed record NightTraceRunResult(bool Accepted, IReadOnlyList<string> Trace, int RemainingBudget);
