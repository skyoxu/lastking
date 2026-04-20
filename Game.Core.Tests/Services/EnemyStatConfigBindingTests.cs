using System;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyStatConfigBindingTests
{
    private const string SelectedEnemyId = "grunt";

    // acceptance: ACC:T38.4
    [Fact]
    public void ShouldExposeUpdatedEnemyStats_WhenOnlyEnemyConfigValuesChange()
    {
        var baselineProjection = ObserveRuntimeEnemyProjection(
            BuildGameplayConfigJson(enemyId: SelectedEnemyId, health: 90m, damage: 12m, speed: 1.5m, spawnCadenceSeconds: 9),
            SelectedEnemyId);
        var updatedProjection = ObserveRuntimeEnemyProjection(
            BuildGameplayConfigJson(enemyId: SelectedEnemyId, health: 98m, damage: 14m, speed: 1.9m, spawnCadenceSeconds: 9),
            SelectedEnemyId);

        baselineProjection.SpawnIntervalSeconds.Should().Be(9);
        updatedProjection.SpawnIntervalSeconds.Should().Be(9);

        updatedProjection.Health.Should().NotBe(baselineProjection.Health);
        updatedProjection.Damage.Should().NotBe(baselineProjection.Damage);
        updatedProjection.MoveSpeed.Should().NotBe(baselineProjection.MoveSpeed);

        updatedProjection.Health.Should().Be(98m);
        updatedProjection.Damage.Should().Be(14m);
        updatedProjection.MoveSpeed.Should().Be(1.9m);
    }

    // acceptance: ACC:T38.10
    [Fact]
    public void ShouldMatchConfiguredEnemyHealthAndSpawnInterval_WhenSelectedEnemyRunIsObserved()
    {
        var observed = ObserveRuntimeEnemyProjection(
            BuildGameplayConfigJson(enemyId: SelectedEnemyId, health: 135m, damage: 11m, speed: 1.45m, spawnCadenceSeconds: 7),
            SelectedEnemyId);

        observed.Health.Should().Be(135m);
        observed.SpawnIntervalSeconds.Should().Be(7);
    }

    // acceptance: ACC:T38.11
    [Fact]
    public void ShouldKeepSpawnIntervalUnchangedButUpdateEnemyHealth_WhenOnlyEnemyConfigChanges()
    {
        var baselineProjection = ObserveRuntimeEnemyProjection(
            BuildGameplayConfigJson(enemyId: SelectedEnemyId, health: 120m, damage: 16m, speed: 1.65m, spawnCadenceSeconds: 11),
            SelectedEnemyId);
        var updatedProjection = ObserveRuntimeEnemyProjection(
            BuildGameplayConfigJson(enemyId: SelectedEnemyId, health: 125m, damage: 16m, speed: 1.65m, spawnCadenceSeconds: 11),
            SelectedEnemyId);

        baselineProjection.SpawnIntervalSeconds.Should().Be(11);
        updatedProjection.SpawnIntervalSeconds.Should().Be(11);
        updatedProjection.Health.Should().NotBe(baselineProjection.Health);
        updatedProjection.Health.Should().Be(125m);
    }

    private static RuntimeEnemyProjection ObserveRuntimeEnemyProjection(string configJson, string enemyId)
    {
        var manager = new ConfigManager();
        var load = manager.LoadInitialFromJson(configJson, $"memory://enemy-config-{Guid.NewGuid():N}.json");
        load.Accepted.Should().BeTrue();

        var resolver = new EnemyConfigRuntimeResolver();
        var runtimeStats = resolver.Resolve(manager, configJson)
            .Single(item => string.Equals(item.EnemyId, enemyId, StringComparison.Ordinal));

        return new RuntimeEnemyProjection(
            EnemyId: runtimeStats.EnemyId,
            Health: runtimeStats.Health,
            Damage: runtimeStats.Damage,
            MoveSpeed: runtimeStats.Speed,
            SpawnIntervalSeconds: manager.Snapshot.SpawnCadenceSeconds);
    }

    private static string BuildGameplayConfigJson(
        string enemyId,
        decimal health,
        decimal damage,
        decimal speed,
        int spawnCadenceSeconds)
    {
        return $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": 120 },
          "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": { "cadence_seconds": {{spawnCadenceSeconds}} },
          "boss": { "count": 2 },
          "battle": { "castle_start_hp": 100 },
          "enemies": [
            {
              "enemy_id": "{{enemyId}}",
              "health": {{health}},
              "damage": {{damage}},
              "speed": {{speed}},
              "enemy_type": "melee",
              "behavior": { "mode": "rush" }
            }
          ]
        }
        """;
    }

    private readonly record struct RuntimeEnemyProjection(
        string EnemyId,
        decimal Health,
        decimal Damage,
        decimal MoveSpeed,
        int SpawnIntervalSeconds);

}
