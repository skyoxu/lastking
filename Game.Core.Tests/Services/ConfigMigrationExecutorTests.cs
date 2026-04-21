using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigMigrationExecutorTests
{
    // ACC:T40.14
    [Fact]
    public void ShouldExecuteMigrationStepsInDeterministicOrder_WhenForceMigrationIsEnabledAndVersionMismatchOccurs()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(
            version: "2.0.0",
            expectedVersion: "1.0.0",
            forceMigration: true,
            migrationStatus: "succeeded",
            migrationRecoverable: true);

        var firstResult = manager.LoadInitialFromJson(json, "res://Config/balance.json");
        var secondResult = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        firstResult.Accepted.Should().BeFalse("mismatched versions must not be partially accepted before deterministic strict-step migration succeeds");
        secondResult.Accepted.Should().BeFalse();

        firstResult.ReasonCodes.Should().Equal(
            "CFG_VERSION_MISMATCH",
            "CFG_VERSION_MIGRATION_REQUIRED",
            "CFG_MIGRATION_STEP_PARSE",
            "CFG_MIGRATION_STEP_VALIDATE",
            "CFG_MIGRATION_STEP_COMMIT");

        secondResult.ReasonCodes.Should().Equal(firstResult.ReasonCodes);
    }

    // ACC:T40.15
    [Fact]
    public void ShouldRejectAndKeepDefaultState_WhenFatalMigrationStepFails()
    {
        var manager = new ConfigManager();
        var json = CreateConfigJson(
            version: "2.0.0",
            expectedVersion: "1.0.0",
            forceMigration: true,
            migrationStatus: "failed",
            migrationRecoverable: false);

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse();
        result.Source.Should().Be("fallback");
        result.Snapshot.Should().Be(BalanceSnapshot.Default);
        manager.Snapshot.Should().Be(BalanceSnapshot.Default);
        manager.ActiveConfigJson.Should().BeEmpty();
        result.ReasonCodes.Should().ContainInOrder(
            "CFG_VERSION_MIGRATION_REQUIRED",
            "CFG_MIGRATION_FATAL");
    }

    private static string CreateConfigJson(
        string version,
        string expectedVersion,
        bool forceMigration,
        string migrationStatus,
        bool migrationRecoverable)
    {
        var forceMigrationLiteral = forceMigration ? "true" : "false";
        var migrationRecoverableLiteral = migrationRecoverable ? "true" : "false";

        return $@"{{
  ""version"": ""{version}"",
  ""expectedVersion"": ""{expectedVersion}"",
  ""forceMigration"": {forceMigrationLiteral},
  ""migration"": {{
    ""status"": ""{migrationStatus}"",
    ""recoverable"": {migrationRecoverableLiteral},
    ""steps"": [
      {{ ""name"": ""parse"", ""strict"": true }},
      {{ ""name"": ""validate"", ""strict"": true }},
      {{ ""name"": ""commit"", ""strict"": true }}
    ]
  }},
  ""time"": {{ ""day_seconds"": 240, ""night_seconds"": 120 }},
  ""waves"": {{ ""normal"": {{ ""day1_budget"": 50, ""daily_growth"": 1.2 }} }},
  ""channels"": {{ ""elite"": ""elite"", ""boss"": ""boss"" }},
  ""spawn"": {{ ""cadence_seconds"": 10 }},
  ""boss"": {{ ""count"": 2 }}
}}";
    }
}
